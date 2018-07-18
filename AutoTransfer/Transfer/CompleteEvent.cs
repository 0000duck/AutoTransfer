﻿using AutoTransfer.Transfer;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using static AutoTransfer.Enum.Ref;
using static AutoTransfer.Transfer.A53;

namespace AutoTransfer.Transfer
{
    public class CompleteEvent
    {
        private IFRS9DBEntities db = new IFRS9DBEntities();
        private Log log = new Log();
        private string A57logPath = string.Empty;
        private string A58logPath = string.Empty;
        private string _user = "System";
        DateTime _date = DateTime.MinValue;
        TimeSpan _time = TimeSpan.MinValue;
        /// <summary>
        /// A53 save 後續動作(save A57,A58)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="version"></param>
        public void saveDb(DateTime dt, int version)
        {
            A57logPath = log.txtLocation(TableType.A57.ToString());
            A58logPath = log.txtLocation(TableType.A58.ToString());
            List<Bond_Rating_Parm> parmIDs = getParmIDs(); //選取有效的D60

            DateTime startTime = DateTime.Now;
            _date = startTime.Date;
            _time = startTime.TimeOfDay;

            if (log.checkTransferCheck(TableType.A57.ToString(), TableType.A53.ToString(), dt, version) &&
                log.checkTransferCheck(TableType.A58.ToString(), TableType.A53.ToString(), dt, version))
            {
                var A41Data = db.Bond_Account_Info
                    .Where(x => x.Report_Date == dt &&
                                 x.Version != null &&
                                 x.Version == version);
                if (A41Data.Any() && db.Rating_Info.Any())
                {
                    using (var dbContextTransaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            #region sql
                            string reportData = dt.ToString("yyyy/MM/dd");
                            string ver = version.ToString();
                            string complement = ConfigurationManager.AppSettings["complement"];
                            string sql = string.Empty; //原始
                            string sql1_2 = string.Empty; //評估日
                            StringBuilder sql_A = new StringBuilder(); //新增三張信評特殊表格
                            string sql_D = string.Empty; //排除多餘
                            string sql1_3 = string.Empty; //更新三張信評特殊表格
                            string sql2 = string.Empty; //A58

                            #region A57原始日 sql
                            var _ratype = Rating_Type.A.GetDescription();
                            var A58d = db.Bond_Rating_Summary.AsNoTracking()
                                .Where(x => x.Report_Date <= dt &&
                                         x.Rating_Type == _ratype)
                                         .OrderByDescending(x => x.Report_Date)
                                         .ThenByDescending(x => x.Version)
                                         .FirstOrDefault();
                            var _lastReport = "null";
                            var _lastVersion = "0";
                            if (A58d != null)
                            {
                                _lastReport = A58d.Report_Date.ToString("yyyy/MM/dd");
                                _lastVersion = A58d.Version.ToString();
                            }
                            sql = $@"
--原始

--WITH temp2 AS
--(
--select top 1 Report_Date,
--             Version
--from Bond_Rating_Summary
--where Report_Date <= '{reportData}'
--and   Rating_Type = '{Rating_Type.A.GetDescription()}'
--group by Report_Date,Version
--order by Report_Date desc,Version desc
--), --最後一版A58
WITH temp AS
(
select RTG_Bloomberg_Field,
       Rating_Object,
       Bond_Number,
       Lots,
       Portfolio_Name,
	   Bond_Number_Old,
	   Lots_Old,
	   Portfolio_Name_Old,
       Rating,
       Rating_Date,
       Rating_Org,
       A57.Report_Date
FROM   Bond_Rating_Info A57
WHERE  A57.Report_Date = '{_lastReport}'
AND    A57.Version = '{_lastVersion}'
AND    A57.Rating_Type = '{Rating_Type.A.GetDescription()}'
AND    A57.Rating is not null
--AND    A57.Grade_Adjust is not null  --排程 要全加 2018/4/11
), --最後一版A57(原始投資信評)
A52 AS (
   SELECT * FROM Grade_Mapping_Info
   Where IsActive = 'Y'
),
A51 AS (
   SELECT * FROM Grade_Moody_Info
   WHERE Status = '1'
),
tempA57t AS (
SELECT _A57.*,
       _A51.Grade_Adjust,
       _A51.PD_Grade
FROM   (select * from temp where Rating_Org <> '{RatingOrg.Moody.GetDescription()}') AS _A57
left Join   A52 _A52
ON     _A57.Rating = _A52.Rating
AND    _A57.Rating_Org = _A52.Rating_Org
left JOIN   A51 _A51
ON     _A52.PD_Grade = _A51.PD_Grade
  UNION ALL
SELECT _A57.*,
       _A51_2.Grade_Adjust,
       _A51_2.PD_Grade
FROM   (select * from temp where Rating_Org = '{RatingOrg.Moody.GetDescription()}' ) AS _A57
left JOIN   A51 _A51_2
ON     _A57.Rating = _A51_2.Rating
),
--最後一版A57(原始投資信評) 加入信評
tempA AS(
   SELECT A41.Reference_Nbr,
          A41.Bond_Number,
          A41.Lots,
          A41.Portfolio,
          A41.Segment_Name,
          A41.Bond_Type,
          A41.Lien_position,
          A41.Origination_Date,
          A41.Report_Date,
		  A41.Portfolio_Name,
		  A41.PRODUCT,
		  A41.ISSUER,
		  A41.Version,
		  A41.ISIN_Changed_Ind,
          A41.Bond_Number_Old,
          A41.Lots_Old,
          A41.Portfolio_Name_Old,
          A41.Origination_Date_Old,
      	  _A53Sample.ISSUER_TICKER,
		  _A53Sample.GUARANTOR_NAME,
		  _A53Sample.GUARANTOR_EQY_TICKER
   FROM (Select *
   from Bond_Account_Info
   where Report_Date = '{reportData}'
   and   Version =  {ver})
   AS A41
   LEFT JOIN (select * from Rating_Info_SampleInfo where Report_Date = '{reportData}') AS _A53Sample
   ON  A41.Bond_Number = _A53Sample.Bond_Number
), --全部的A41
T1 AS (
   SELECT BA_Info.Reference_Nbr AS Reference_Nbr ,
          BA_Info.Bond_Number AS Bond_Number,
		  BA_Info.Lots AS Lots,
		  BA_Info.Portfolio AS Portfolio,
		  BA_Info.Segment_Name AS Segment_Name,
		  BA_Info.Bond_Type AS Bond_Type,
		  BA_Info.Lien_position AS Lien_position,
		  BA_Info.Origination_Date AS Origination_Date,
          BA_Info.Report_Date AS Report_Date,
		  oldA57.Rating_Date AS Rating_Date,
          CASE WHEN oldA57.Rating_Object is null
               THEN ' '
               ELSE oldA57.Rating_Object
          END AS Rating_Object,
          oldA57.Rating_Org AS Rating_Org,
		  oldA57.Rating AS Rating,
		  (CASE WHEN oldA57.Rating_Org in ('{RatingOrg.SP.GetDescription()}','{RatingOrg.Moody.GetDescription()}','{RatingOrg.Fitch.GetDescription()}') THEN '國外'
	            WHEN oldA57.Rating_Org in ('{RatingOrg.FitchTwn.GetDescription()}','{RatingOrg.CW.GetDescription()}') THEN '國內'
                ELSE ' '
	      END) AS Rating_Org_Area,
          oldA57.PD_Grade,
          oldA57.Grade_Adjust,
		  CASE WHEN BA_Info.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.ISSUER_TICKER END AS ISSUER_TICKER,
		  CASE WHEN BA_Info.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_NAME END AS GUARANTOR_NAME,
		  CASE WHEN BA_Info.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
		  BA_Info.Portfolio_Name AS Portfolio_Name,
          CASE WHEN oldA57.RTG_Bloomberg_Field is null
               THEN ' '
               ELSE oldA57.RTG_Bloomberg_Field
          END AS RTG_Bloomberg_Field,
		  BA_Info.PRODUCT AS SMF,
		  BA_Info.ISSUER AS ISSUER,
		  BA_Info.Version AS Version,
		  (CASE WHEN BA_Info.PRODUCT like 'A11%' OR BA_Info.PRODUCT like '932%'
		  THEN BA_Info.Bond_Number + ' Mtge' ELSE
		    BA_Info.Bond_Number + ' Corp' END) AS Security_Ticker,
          BA_Info.ISIN_Changed_Ind AS ISIN_Changed_Ind,
          BA_Info.Bond_Number_Old AS Bond_Number_Old,
          BA_Info.Lots_Old AS Lots_Old,
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old,
          BA_Info.Origination_Date_Old AS Origination_Date_Old
   FROM  (select * from tempA where ISIN_Changed_Ind is null) AS BA_Info --沒換券的A41
   JOIN  tempA57t oldA57 --oldA57
   ON    BA_Info.Bond_Number =  oldA57.Bond_Number
   AND   BA_Info.Lots = oldA57.Lots
   AND   BA_Info.Portfolio_Name = oldA57.Portfolio_Name 
UNION ALL
   SELECT BA_Info.Reference_Nbr AS Reference_Nbr ,
          BA_Info.Bond_Number AS Bond_Number,
		  BA_Info.Lots AS Lots,
		  BA_Info.Portfolio AS Portfolio,
		  BA_Info.Segment_Name AS Segment_Name,
		  BA_Info.Bond_Type AS Bond_Type,
		  BA_Info.Lien_position AS Lien_position,
		  BA_Info.Origination_Date AS Origination_Date,
          BA_Info.Report_Date AS Report_Date,
		  oldA57.Rating_Date AS Rating_Date,
          CASE WHEN oldA57.Rating_Object is null
               THEN ' '
               ELSE oldA57.Rating_Object
          END AS Rating_Object,
          oldA57.Rating_Org AS Rating_Org,
		  oldA57.Rating AS Rating,
		  (CASE WHEN oldA57.Rating_Org in ('{RatingOrg.SP.GetDescription()}','{RatingOrg.Moody.GetDescription()}','{RatingOrg.Fitch.GetDescription()}') THEN '國外'
	            WHEN oldA57.Rating_Org in ('{RatingOrg.FitchTwn.GetDescription()}','{RatingOrg.CW.GetDescription()}') THEN '國內'
                ELSE ' '
	      END) AS Rating_Org_Area,
          oldA57.PD_Grade,
          oldA57.Grade_Adjust,
		  CASE WHEN BA_Info.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.ISSUER_TICKER END AS ISSUER_TICKER,
		  CASE WHEN BA_Info.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_NAME END AS GUARANTOR_NAME,
		  CASE WHEN BA_Info.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
		  BA_Info.Portfolio_Name AS Portfolio_Name,
          CASE WHEN oldA57.RTG_Bloomberg_Field is null
               THEN ' '
               ELSE oldA57.RTG_Bloomberg_Field
          END AS RTG_Bloomberg_Field,
		  BA_Info.PRODUCT AS SMF,
		  BA_Info.ISSUER AS ISSUER,
		  BA_Info.Version AS Version,
		  (CASE WHEN BA_Info.PRODUCT like 'A11%' OR BA_Info.PRODUCT like '932%'
		  THEN BA_Info.Bond_Number + ' Mtge' ELSE
		    BA_Info.Bond_Number + ' Corp' END) AS Security_Ticker,
          BA_Info.ISIN_Changed_Ind AS ISIN_Changed_Ind,
          BA_Info.Bond_Number_Old AS Bond_Number_Old,
          BA_Info.Lots_Old AS Lots_Old,
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old,
          BA_Info.Origination_Date_Old AS Origination_Date_Old
   FROM  (select * from tempA where ISIN_Changed_Ind is null) AS BA_Info --沒換券的A41
   JOIN  (select * from tempA57t where Bond_Number_Old is not null) AS oldA57 --oldA57
   ON    BA_Info.Bond_Number =  oldA57.Bond_Number_Old
   AND   BA_Info.Lots = oldA57.Lots_Old
   AND   BA_Info.Portfolio_Name = oldA57.Portfolio_Name_Old
UNION ALL
   SELECT BA_Info.Reference_Nbr AS Reference_Nbr ,
          BA_Info.Bond_Number AS Bond_Number,
		  BA_Info.Lots AS Lots,
		  BA_Info.Portfolio AS Portfolio,
		  BA_Info.Segment_Name AS Segment_Name,
		  BA_Info.Bond_Type AS Bond_Type,
		  BA_Info.Lien_position AS Lien_position,
		  BA_Info.Origination_Date AS Origination_Date,
          BA_Info.Report_Date AS Report_Date,
		  oldA57.Rating_Date AS Rating_Date,
          CASE WHEN oldA57.Rating_Object is null
               THEN ' '
               ELSE oldA57.Rating_Object
          END AS Rating_Object,
          oldA57.Rating_Org AS Rating_Org,
		  oldA57.Rating AS Rating,
		  (CASE WHEN oldA57.Rating_Org in ('{RatingOrg.SP.GetDescription()}','{RatingOrg.Moody.GetDescription()}','{RatingOrg.Fitch.GetDescription()}') THEN '國外'
	            WHEN oldA57.Rating_Org in ('{RatingOrg.FitchTwn.GetDescription()}','{RatingOrg.CW.GetDescription()}') THEN '國內'
                ELSE ' '
	      END) AS Rating_Org_Area,
          oldA57.PD_Grade,
          oldA57.Grade_Adjust,
          CASE WHEN BA_Info.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.ISSUER_TICKER END AS ISSUER_TICKER,
          CASE WHEN BA_Info.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_NAME END AS GUARANTOR_NAME,
          CASE WHEN BA_Info.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
		  BA_Info.Portfolio_Name AS Portfolio_Name,
          CASE WHEN oldA57.RTG_Bloomberg_Field is null
               THEN ' '
               ELSE oldA57.RTG_Bloomberg_Field
          END AS RTG_Bloomberg_Field,
		  BA_Info.PRODUCT AS SMF,
		  BA_Info.ISSUER AS ISSUER,
		  BA_Info.Version AS Version,
		  (CASE WHEN BA_Info.PRODUCT like 'A11%' OR BA_Info.PRODUCT like '932%'
		  THEN BA_Info.Bond_Number + ' Mtge' ELSE
		    BA_Info.Bond_Number + ' Corp' END) AS Security_Ticker,
          BA_Info.ISIN_Changed_Ind AS ISIN_Changed_Ind,
          BA_Info.Bond_Number_Old AS Bond_Number_Old,
          BA_Info.Lots_Old AS Lots_Old,
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old,
          BA_Info.Origination_Date_Old AS Origination_Date_Old
   FROM  (select * from tempA where ISIN_Changed_Ind = 'Y') AS BA_Info --換券的A41
   JOIN  tempA57t oldA57 --oldA57
   ON    BA_Info.Bond_Number_Old = oldA57.Bond_Number
   AND   BA_Info.Lots_Old = oldA57.Lots
   AND   BA_Info.Portfolio_Name_Old = oldA57.Portfolio_Name
UNION ALL
   SELECT BA_Info.Reference_Nbr AS Reference_Nbr ,
          BA_Info.Bond_Number AS Bond_Number,
		  BA_Info.Lots AS Lots,
		  BA_Info.Portfolio AS Portfolio,
		  BA_Info.Segment_Name AS Segment_Name,
		  BA_Info.Bond_Type AS Bond_Type,
		  BA_Info.Lien_position AS Lien_position,
		  BA_Info.Origination_Date AS Origination_Date,
          BA_Info.Report_Date AS Report_Date,
		  oldA57.Rating_Date AS Rating_Date,
          CASE WHEN oldA57.Rating_Object is null
               THEN ' '
               ELSE oldA57.Rating_Object
          END AS Rating_Object,
          oldA57.Rating_Org AS Rating_Org,
		  oldA57.Rating AS Rating,
		  (CASE WHEN oldA57.Rating_Org in ('{RatingOrg.SP.GetDescription()}','{RatingOrg.Moody.GetDescription()}','{RatingOrg.Fitch.GetDescription()}') THEN '國外'
	            WHEN oldA57.Rating_Org in ('{RatingOrg.FitchTwn.GetDescription()}','{RatingOrg.CW.GetDescription()}') THEN '國內'
                ELSE ' '
	      END) AS Rating_Org_Area,
          oldA57.PD_Grade,
          oldA57.Grade_Adjust,
          CASE WHEN BA_Info.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.ISSUER_TICKER END AS ISSUER_TICKER,
          CASE WHEN BA_Info.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_NAME END AS GUARANTOR_NAME,
          CASE WHEN BA_Info.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
		  BA_Info.Portfolio_Name AS Portfolio_Name,
          CASE WHEN oldA57.RTG_Bloomberg_Field is null
               THEN ' '
               ELSE oldA57.RTG_Bloomberg_Field
          END AS RTG_Bloomberg_Field,
		  BA_Info.PRODUCT AS SMF,
		  BA_Info.ISSUER AS ISSUER,
		  BA_Info.Version AS Version,
		  (CASE WHEN BA_Info.PRODUCT like 'A11%' OR BA_Info.PRODUCT like '932%'
		  THEN BA_Info.Bond_Number + ' Mtge' ELSE
		    BA_Info.Bond_Number + ' Corp' END) AS Security_Ticker,
          BA_Info.ISIN_Changed_Ind AS ISIN_Changed_Ind,
          BA_Info.Bond_Number_Old AS Bond_Number_Old,
          BA_Info.Lots_Old AS Lots_Old,
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old,
          BA_Info.Origination_Date_Old AS Origination_Date_Old
   FROM  (select * from tempA where ISIN_Changed_Ind = 'Y') AS BA_Info --換券的A41
   JOIN  (select * from tempA57t where Bond_Number_Old is not null) AS oldA57 --oldA57
   ON    BA_Info.Bond_Number_Old =  oldA57.Bond_Number_Old
   AND   BA_Info.Lots_Old = oldA57.Lots_Old
   AND   BA_Info.Portfolio_Name_Old = oldA57.Portfolio_Name_Old
),
T1s AS(
Select BA_Info.Reference_Nbr AS Reference_Nbr ,
    BA_Info.Bond_Number AS Bond_Number,
    BA_Info.Lots AS Lots,
    BA_Info.Portfolio AS Portfolio,
    BA_Info.Segment_Name AS Segment_Name,
    BA_Info.Bond_Type AS Bond_Type,
    BA_Info.Lien_position AS Lien_position,
    BA_Info.Origination_Date AS Origination_Date,
    BA_Info.Report_Date AS Report_Date,
    null AS Rating_Date,
    ' '  AS Rating_Object,
    null AS Rating_Org,
    null AS Rating,
    ' '  AS Rating_Org_Area,
    null AS PD_Grade,
    null AS Grade_Adjust,
    CASE WHEN BA_Info.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.ISSUER_TICKER END AS ISSUER_TICKER,
    CASE WHEN BA_Info.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_NAME END AS GUARANTOR_NAME,
    CASE WHEN BA_Info.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
    BA_Info.Portfolio_Name AS Portfolio_Name,
    ' '  AS RTG_Bloomberg_Field,
    BA_Info.PRODUCT AS SMF,
    BA_Info.ISSUER AS ISSUER,
    BA_Info.Version AS Version,
    CASE WHEN BA_Info.PRODUCT like 'A11%' OR BA_Info.PRODUCT like '932%'
         THEN BA_Info.Bond_Number + ' Mtge'
	    ELSE BA_Info.Bond_Number + ' Corp'
    END AS Security_Ticker,
    BA_Info.ISIN_Changed_Ind AS ISIN_Changed_Ind,
    BA_Info.Bond_Number_Old AS Bond_Number_Old,
    BA_Info.Lots_Old AS Lots_Old,
    BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old,
    BA_Info.Origination_Date_Old AS Origination_Date_Old
from tempA BA_Info
),
T1all AS(
  select T1.*, D60.Parm_ID AS Parm_ID from T1
  left join
  (select * from Bond_Rating_Parm where Status = '2' and IsActive = 'Y') D60
  on T1.Rating_Object = D60.Rating_Object
  and T1.Rating_Org_Area = 
      case when D60.Rating_Org_Area is null
  	     then T1.Rating_Org_Area
  		 else D60.Rating_Org_Area
      end
  UNION ALL
  select *, null AS Parm_ID from T1s
)
INSERT INTO Bond_Rating_Info
           (Reference_Nbr,
           Bond_Number,
           Lots,
           Portfolio,
           Segment_Name,
           Bond_Type,
           Lien_position,
           Origination_Date,
           Report_Date,
           Rating_Date,
           Rating_Type,
           Rating_Object,
           Rating_Org,
           Rating,
           Rating_Org_Area,
           PD_Grade,
           Grade_Adjust,
           ISSUER_TICKER,
           GUARANTOR_NAME,
           GUARANTOR_EQY_TICKER,
           Parm_ID,
           Portfolio_Name,
           RTG_Bloomberg_Field,
           SMF,
           ISSUER,
           Version,
           Security_Ticker,
           ISIN_Changed_Ind,
           Bond_Number_Old,
           Lots_Old,
           Portfolio_Name_Old,
           Origination_Date_Old,
           Create_User,
           Create_Date,
           Create_Time
)
SELECT     Reference_Nbr,
		   Bond_Number,
		   Lots,
           Portfolio,
           Segment_Name,
           Bond_Type,
           Lien_position,
           Origination_Date,
           Report_Date,
           Rating_Date,
           '{Rating_Type.A.GetDescription()}',
           Rating_Object,
           Rating_Org,
           Rating,
           Rating_Org_Area,
           PD_Grade,
           Grade_Adjust,
           ISSUER_TICKER,
           GUARANTOR_NAME,
           GUARANTOR_EQY_TICKER,
           Parm_ID,
           Portfolio_Name,
           RTG_Bloomberg_Field,
           SMF,
           ISSUER,
           Version,
           Security_Ticker,
           ISIN_Changed_Ind,
           Bond_Number_Old,
           Lots_Old,
           Portfolio_Name_Old,
           Origination_Date_Old,
           {_user.stringToStrSql()},
           {_date.dateTimeToStrSql()},
           {_time.timeSpanToStrSql()}
		   From
		   T1all ; ";

                            //刪除A57(原始)預設
                            sql_D += $@"
with tempDeleA as
(
Select distinct Reference_Nbr from Bond_Rating_Info
where  Report_Date = '{reportData}'
and Version = {ver}
and Rating_Type = '{Rating_Type.A.GetDescription()}'
and Rating is not null
)
delete Bond_Rating_Info
where  Report_Date = '{reportData}'
and Version = {ver}
and Rating is null
and Rating_Type = '{Rating_Type.A.GetDescription()}'
and Reference_Nbr in (select Reference_Nbr from tempDeleA); ";

                            #endregion A57原始日 sql

                            #region A57評估日 sql

                            sql1_2 = $@"
 --評估日

WITH A52 AS (
   SELECT * FROM Grade_Mapping_Info
   Where IsActive = 'Y'
),
A51 AS (
   SELECT * FROM Grade_Moody_Info
   WHERE Status = '1'
),
A53 AS (
  SELECT  Bond_Number,
          Rating_Date,
          Rating_Org,
		  Rating,
		  Rating_Object,
		  RTG_Bloomberg_Field
    FROM  Rating_Info
    WHERE Report_Date = '{reportData}'
    AND   RTG_Bloomberg_Field not in ('G_RTG_MDY_LOCAL_LT_BANK_DEPOSITS','RTG_MDY_LOCAL_LT_BANK_DEPOSITS') --穆迪長期本國銀行存款評等,在寫A57時不要寫進去放成空值,風控暫時不用它來判斷熟調或熟低。
),
A53t AS (
SELECT _A53.*,
       _A51.Grade_Adjust,
       _A51.PD_Grade
FROM   A53 _A53
left Join   A52 _A52
ON     _A53.Rating_Org <> '{RatingOrg.Moody.GetDescription()}'
AND    _A53.Rating_Org = _A52.Rating_Org
AND    _A53.Rating = _A52.Rating
left JOIN   A51 _A51
ON     _A52.PD_Grade = _A51.PD_Grade
WHERE  _A53.Rating_Org <> '{RatingOrg.Moody.GetDescription()}'
  UNION ALL
SELECT _A53.*,
       _A51_2.Grade_Adjust,
       _A51_2.PD_Grade
FROM   A53 _A53
left JOIN   A51 _A51_2
ON     _A53.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
AND    _A53.Rating = _A51_2.Rating
WHERE  _A53.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
),
tempC AS (
   SELECT   A41.Reference_Nbr,
            A41.Bond_Number,
            A41.Lots,
            A41.Portfolio,
            A41.Segment_Name,
            A41.Bond_Type,
            A41.Lien_position,
            A41.Origination_Date,
            A41.Report_Date,
		    A41.Portfolio_Name,
		    A41.PRODUCT,
		    A41.ISSUER,
		    A41.Version,
		    A41.ISIN_Changed_Ind,
            A41.Bond_Number_Old,
            A41.Lots_Old,
            A41.Portfolio_Name_Old,
            A41.Origination_Date_Old,
	        _A53Sample.ISSUER_TICKER,
			_A53Sample.GUARANTOR_NAME,
			_A53Sample.GUARANTOR_EQY_TICKER
	 FROM (select * from Bond_Account_Info
     where Report_Date = '{reportData}'
     and VERSION = {ver}
     ) AS A41
	 LEFT JOIN Rating_Info_SampleInfo _A53Sample
	 ON   A41.Bond_Number = _A53Sample.Bond_Number
     AND  _A53Sample.Report_Date = '{reportData}'
),
T0 AS (
   SELECT BA_Info.Reference_Nbr AS Reference_Nbr ,
          BA_Info.Bond_Number AS Bond_Number,
		  BA_Info.Lots AS Lots,
		  BA_Info.Portfolio AS Portfolio,
		  BA_Info.Segment_Name AS Segment_Name,
		  BA_Info.Bond_Type AS Bond_Type,
		  BA_Info.Lien_position AS Lien_position,
		  BA_Info.Origination_Date AS Origination_Date,
          BA_Info.Report_Date AS Report_Date,
		  RA_Info.Rating_Date AS Rating_Date,
          CASE WHEN RA_Info.Rating_Object is null
               THEN ' '
               ELSE RA_Info.Rating_Object
          END AS Rating_Object,
          RA_Info.Rating_Org AS Rating_Org,
		  RA_Info.Rating AS Rating,
		  (CASE WHEN RA_Info.Rating_Org in ('{RatingOrg.SP.GetDescription()}','{RatingOrg.Moody.GetDescription()}','{RatingOrg.Fitch.GetDescription()}') THEN '國外'
	            WHEN RA_Info.Rating_Org in ('{RatingOrg.FitchTwn.GetDescription()}','{RatingOrg.CW.GetDescription()}') THEN '國內'
                ELSE ' '
	      END) AS Rating_Org_Area,
          RA_Info.PD_Grade,
          RA_Info.Grade_Adjust,
		  CASE WHEN BA_Info.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.ISSUER_TICKER END AS ISSUER_TICKER,
		  CASE WHEN BA_Info.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_NAME END AS GUARANTOR_NAME,
		  CASE WHEN BA_Info.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
		  BA_Info.Portfolio_Name AS Portfolio_Name,
		  CASE WHEN RA_Info.RTG_Bloomberg_Field is null
               THEN ' '
               ELSE RA_Info.RTG_Bloomberg_Field
          END AS RTG_Bloomberg_Field,
		  BA_Info.PRODUCT AS SMF,
		  BA_Info.ISSUER AS ISSUER,
		  BA_Info.Version AS Version,
		  (CASE WHEN BA_Info.PRODUCT like 'A11%' OR BA_Info.PRODUCT like '932%'
		  THEN BA_Info.Bond_Number + ' Mtge' ELSE
		    BA_Info.Bond_Number + ' Corp' END) AS Security_Ticker,
          BA_Info.ISIN_Changed_Ind AS ISIN_Changed_Ind,
          BA_Info.Bond_Number_Old AS Bond_Number_Old,
          BA_Info.Lots_Old AS Lots_Old,
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old,
          BA_Info.Origination_Date_Old AS Origination_Date_Old
   FROM  ( Select * from tempC ) BA_Info --A41  
   JOIN A53t RA_Info --A53
   ON BA_Info.Bond_Number = RA_Info.Bond_Number
   UNION ALL
   SELECT BA_Info.Reference_Nbr AS Reference_Nbr ,
          BA_Info.Bond_Number AS Bond_Number,
		  BA_Info.Lots AS Lots,
		  BA_Info.Portfolio AS Portfolio,
		  BA_Info.Segment_Name AS Segment_Name,
		  BA_Info.Bond_Type AS Bond_Type,
		  BA_Info.Lien_position AS Lien_position,
		  BA_Info.Origination_Date AS Origination_Date,
          BA_Info.Report_Date AS Report_Date,
		  null,
          ' ' AS Rating_Object,
          null,
		  null,
		  ' ' AS Rating_Org_Area,
          null,
          null,
		  CASE WHEN BA_Info.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.ISSUER_TICKER END AS ISSUER_TICKER,
		  CASE WHEN BA_Info.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_NAME END AS GUARANTOR_NAME,
		  CASE WHEN BA_Info.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else BA_Info.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
		  BA_Info.Portfolio_Name AS Portfolio_Name,
		  ' ' AS RTG_Bloomberg_Field,
		  BA_Info.PRODUCT AS SMF,
		  BA_Info.ISSUER AS ISSUER,
		  BA_Info.Version AS Version,
		  (CASE WHEN BA_Info.PRODUCT like 'A11%' OR BA_Info.PRODUCT like '932%'
		  THEN BA_Info.Bond_Number + ' Mtge' ELSE
		    BA_Info.Bond_Number + ' Corp' END) AS Security_Ticker,
          BA_Info.ISIN_Changed_Ind AS ISIN_Changed_Ind,
          BA_Info.Bond_Number_Old AS Bond_Number_Old,
          BA_Info.Lots_Old AS Lots_Old,
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old,
          BA_Info.Origination_Date_Old AS Origination_Date_Old
   FROM  tempC BA_Info --A41
)
Insert into Bond_Rating_Info
           (Reference_Nbr,
           Bond_Number,
           Lots,
           Portfolio,
           Segment_Name,
           Bond_Type,
           Lien_position,
           Origination_Date,
           Report_Date,
           Rating_Date,
           Rating_Type,
           Rating_Object,
           Rating_Org,
           Rating,
           Rating_Org_Area,
           PD_Grade,
           Grade_Adjust,
           ISSUER_TICKER,
           GUARANTOR_NAME,
           GUARANTOR_EQY_TICKER,
           Parm_ID,
           Portfolio_Name,
           RTG_Bloomberg_Field,
           SMF,
           ISSUER,
           Version,
           Security_Ticker,
           ISIN_Changed_Ind,
           Bond_Number_Old,
           Lots_Old,
           Portfolio_Name_Old,
           Origination_Date_Old,
           Create_User,
           Create_Date,
           Create_Time)
SELECT     T0.Reference_Nbr,
		   T0.Bond_Number,
		   T0.Lots,
           T0.Portfolio,
           T0.Segment_Name,
           T0.Bond_Type,
           T0.Lien_position,
           T0.Origination_Date,
           T0.Report_Date,
           T0.Rating_Date,
           '{Rating_Type.B.GetDescription()}',
           T0.Rating_Object,
           T0.Rating_Org,
           T0.Rating,
           T0.Rating_Org_Area,
           T0.PD_Grade,
           T0.Grade_Adjust,
           T0.ISSUER_TICKER,
           T0.GUARANTOR_NAME,
           T0.GUARANTOR_EQY_TICKER,
           D60.Parm_ID,
           T0.Portfolio_Name,
           T0.RTG_Bloomberg_Field,
           T0.SMF,
           T0.ISSUER,
           T0.Version,
           T0.Security_Ticker,
           T0.ISIN_Changed_Ind,
           T0.Bond_Number_Old,
           T0.Lots_Old,
           T0.Portfolio_Name_Old,
           T0.Origination_Date_Old,
           {_user.stringToStrSql()},
           {_date.dateTimeToStrSql()},
           {_time.timeSpanToStrSql()}
		   From T0
  left join
  (select * from Bond_Rating_Parm where Status = '2' and IsActive = 'Y') D60
  on T0.Rating_Object = D60.Rating_Object
  and T0.Rating_Org_Area = 
      case when D60.Rating_Org_Area is null
  	     then T0.Rating_Org_Area
  		 else D60.Rating_Org_Area
      end
; ";

                            //刪除A57(評估日)預設
                            sql_D += $@"
with tempDeleB as
(
Select distinct Reference_Nbr from Bond_Rating_Info
where  Report_Date = '{reportData}'
and Version = {ver}
and Rating_Type = '{Rating_Type.B.GetDescription()}'
and Rating is not null
)
delete Bond_Rating_Info
where  Report_Date = '{reportData}'
and Version = {ver}
and Rating is null
and Rating_Type = '{Rating_Type.B.GetDescription()}'
and Reference_Nbr in (select Reference_Nbr from tempDeleB);
";

                            #endregion A57評估日 sql

                            #region 三張特殊表單更新 sql

                            sql1_3 = $@"
                            -- update Bond_Rating(債項信評) 的設定
                            update Bond_Rating_Info
                            set Rating =
                                  case
                            	    when Bond_Rating_Info.Rating_Org = '{RatingOrg.SP.GetDescription()}'
                            	     then Bond_Rating.S_And_P
                                    when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                            	     then Bond_Rating.Moodys
                            		when Bond_Rating_Info.Rating_Org = '{RatingOrg.Fitch.GetDescription()}'
                            		 then Bond_Rating.Fitch
                            		when Bond_Rating_Info.Rating_Org = '{RatingOrg.FitchTwn.GetDescription()}'
                            		 then Bond_Rating.Fitch_TW
                            		when  Bond_Rating_Info.Rating_Org = '{RatingOrg.CW.GetDescription()}'
                            		 then Bond_Rating.TRC
                            	  else Bond_Rating_Info.Rating
                            	  end,
                                Rating_Date = null
                            from Bond_Rating
                            where  Bond_Rating_Info.Bond_Number = Bond_Rating.Bond_Number
                            and Bond_Rating_Info.Report_Date = '{reportData}'
                            and Bond_Rating_Info.Version = {ver}
                            and Bond_Rating_Info.Rating_Object = '{RatingObject.Bonds.GetDescription()}'
                            and Bond_Rating_Info.Rating_Type = '{Rating_Type.B.GetDescription()}' ;

                            update Bond_Rating_Info
                            set PD_Grade =
                                   case
                                     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                                     then (select top 1 PD_Grade from Grade_Moody_Info A51 where A51.Status = '1' and A51.Rating = Bond_Rating_Info.Rating)
                            	     else (select top 1 PD_Grade from
                            	       Grade_Moody_Info A51
                            		   where A51.Status = '1' and A51.PD_Grade =
                            	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating and A52.IsActive = 'Y'))
                                   end ,
                                Grade_Adjust =
                                     case
                            	     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                            		 then (select top 1 Grade_Adjust from Grade_Moody_Info A51 where A51.Status = '1' and A51.Rating = Bond_Rating_Info.Rating)
                            		 else (select top 1 Grade_Adjust from
                            	       Grade_Moody_Info A51
                            		   where A51.Status = '1' and A51.PD_Grade =
                            	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating and A52.IsActive = 'Y'))
                                  end
                            from Bond_Rating
                            where  Bond_Rating_Info.Bond_Number = Bond_Rating.Bond_Number
                            and Bond_Rating_Info.Report_Date = '{reportData}'
                            and Bond_Rating_Info.Version = {ver}
                            and Bond_Rating_Info.Rating_Object = '{RatingObject.Bonds.GetDescription()}'
                            and Bond_Rating_Info.Rating_Type = '{Rating_Type.B.GetDescription()}' ;

                            -- update Issuer_Rating(發行者信評) 的設定
                            update Bond_Rating_Info
                            set Rating =
                                  case
                            	    when Bond_Rating_Info.Rating_Org = '{RatingOrg.SP.GetDescription()}'
                            	     then Issuer_Rating.S_And_P
                                    when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                            	     then Issuer_Rating.Moodys
                            		when Bond_Rating_Info.Rating_Org = '{RatingOrg.Fitch.GetDescription()}'
                            		 then Issuer_Rating.Fitch
                            		when Bond_Rating_Info.Rating_Org = '{RatingOrg.FitchTwn.GetDescription()}'
                            		 then Issuer_Rating.Fitch_TW
                            		when  Bond_Rating_Info.Rating_Org = '{RatingOrg.CW.GetDescription()}'
                            		 then Issuer_Rating.TRC
                            	  else Bond_Rating_Info.Rating
                            	  end,
                                Rating_Date = null
                            from Issuer_Rating
                            where  Bond_Rating_Info.ISSUER = Issuer_Rating.Issuer
                            and Bond_Rating_Info.Report_Date = '{reportData}'
                            and Bond_Rating_Info.Version = {ver}
                            and Bond_Rating_Info.Rating_Object = '{RatingObject.ISSUER.GetDescription()}'
                            and Bond_Rating_Info.Rating_Type = '{Rating_Type.B.GetDescription()}' ;

                            update Bond_Rating_Info
                            set PD_Grade =
                                   case
                                     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                                     then (select top 1 PD_Grade from Grade_Moody_Info A51 where A51.Status = '1' and A51.Rating = Bond_Rating_Info.Rating)
                            	     else (select top 1 PD_Grade from
                            	       Grade_Moody_Info A51
                            		   where A51.Status = '1' and A51.PD_Grade =
                            	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating and A52.IsActive = 'Y'))
                                   end ,
                                Grade_Adjust =
                                     case
                            	     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                            		 then (select top 1 Grade_Adjust from Grade_Moody_Info A51 where A51.Status = '1' and A51.Rating = Bond_Rating_Info.Rating)
                            		 else (select top 1 Grade_Adjust from
                            	       Grade_Moody_Info A51
                            		   where A51.Status = '1' and A51.PD_Grade =
                            	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating and A52.IsActive = 'Y'))
                                  end
                            from Issuer_Rating
                            where  Bond_Rating_Info.ISSUER = Issuer_Rating.Issuer
                            and Bond_Rating_Info.Report_Date = '{reportData}'
                            and Bond_Rating_Info.Version = {ver}
                            and Bond_Rating_Info.Rating_Object = '{RatingObject.ISSUER.GetDescription()}'
                            and Bond_Rating_Info.Rating_Type = '{Rating_Type.B.GetDescription()}' ;

                            -- update Guarantor_Rating(擔保者信評) 的設定
                            update Bond_Rating_Info
                            set Rating =
                                  case
                            	    when Bond_Rating_Info.Rating_Org = '{RatingOrg.SP.GetDescription()}' 
                            	     then Guarantor_Rating.S_And_P
                                    when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}' 
                            	     then Guarantor_Rating.Moodys
                            		when Bond_Rating_Info.Rating_Org = '{RatingOrg.Fitch.GetDescription()}' 
                            		 then Guarantor_Rating.Fitch
                            		when Bond_Rating_Info.Rating_Org = '{RatingOrg.FitchTwn.GetDescription()}' 
                            		 then Guarantor_Rating.Fitch_TW
                            		when  Bond_Rating_Info.Rating_Org = '{RatingOrg.CW.GetDescription()}' 
                            		 then Guarantor_Rating.TRC
                            	  else Bond_Rating_Info.Rating
                            	  end,
                                Rating_Date = null
                            from Guarantor_Rating
                            where  Bond_Rating_Info.ISSUER = Guarantor_Rating.Issuer
                            and Bond_Rating_Info.Report_Date = '{reportData}'
                            and Bond_Rating_Info.Version = {ver}
                            and Bond_Rating_Info.Rating_Object = '{RatingObject.GUARANTOR.GetDescription()}'
                            and Bond_Rating_Info.Rating_Type = '{Rating_Type.B.GetDescription()}' ;

                            update Bond_Rating_Info
                            set PD_Grade =
                                   case
                                     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                                     then (select top 1 PD_Grade from Grade_Moody_Info A51 where A51.Status = '1' and A51.Rating = Bond_Rating_Info.Rating)
                            	     else (select top 1 PD_Grade from
                            	       Grade_Moody_Info A51
                            		   where A51.Status = '1' and A51.PD_Grade =
                            	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating and A52.IsActive = 'Y'))
                                   end ,
                                Grade_Adjust =
                                     case
                            	     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
                            		 then (select top 1 Grade_Adjust from Grade_Moody_Info A51 where A51.Status = '1' and A51.Rating = Bond_Rating_Info.Rating)
                            		 else (select top 1 Grade_Adjust from
                            	       Grade_Moody_Info A51
                            		   where A51.Status = '1' and A51.PD_Grade =
                            	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating and A52.IsActive = 'Y'))
                                  end
                            from Guarantor_Rating
                            where  Bond_Rating_Info.ISSUER = Guarantor_Rating.Issuer
                            and Bond_Rating_Info.Report_Date = '{reportData}'
                            and Bond_Rating_Info.Version = {ver}
                            and Bond_Rating_Info.Rating_Object = '{RatingObject.GUARANTOR.GetDescription()}'
                            and Bond_Rating_Info.Rating_Type = '{Rating_Type.B.GetDescription()}' ;

                            ";

                            #endregion 三張特殊表單更新 sql

                            #region A58 sql

                            sql2 = $@"
WITH A57 AS
(
  SELECT * FROM Bond_Rating_Info
  WHERE Report_Date = '{reportData}'
  AND Version = {ver}
),
T4 AS
(
Select BR_Info.Reference_Nbr,
       BR_Info.Report_Date,
	   BR_Info.Parm_ID,
	   BR_Info.Bond_Type,
	   BR_Info.Rating_Type,
	   BR_Info.Rating_Object,
	   BR_Info.Rating_Org_Area,
	   BR_Parm.Rating_Selection,
	   (CASE WHEN BR_Parm.Rating_Selection = '1'
	         THEN Min(BR_Info.Grade_Adjust)
			 WHEN BR_Parm.Rating_Selection = '2'
			 THEN Max(BR_Info.Grade_Adjust)
			 ELSE null  END) AS Grade_Adjust,
	   BR_Parm.Rating_Priority,
       '{startTime.ToString("yyyy/MM/dd")}' AS Processing_Date,
	   BR_Info.Version,
	   BR_Info.Bond_Number,
	   BR_Info.Lots,
	   BR_Info.Portfolio,
	   BR_Info.Origination_Date,
	   BR_Info.Portfolio_Name,
	   BR_Info.SMF,
	   BR_Info.ISSUER,
       BR_Info.ISIN_Changed_Ind,
       BR_Info.Bond_Number_Old,
       BR_Info.Lots_Old,
       BR_Info.Portfolio_Name_Old,
       BR_Info.Origination_Date_Old
From   A57 BR_Info
LEFT JOIN (select * from  Bond_Rating_Parm where Status = '2' and IsActive = 'Y') BR_Parm
ON      BR_Info.Rating_Object = BR_Parm.Rating_Object  
AND     BR_Info.Parm_ID =  BR_Parm.Parm_ID
AND     BR_Info.Rating_Org_Area =
        CASE WHEN BR_Parm.Rating_Org_Area is null
             THEN BR_Info.Rating_Org_Area
             ELSE BR_Parm.Rating_Org_Area
        END 
--on         BR_Info.Parm_ID = BR_Parm.Parm_ID --2018/03/09 Parm_ID 改為流水號
--AND        BR_Info.Rating_Object = BR_Parm.Rating_Object
--AND        BR_Info.Rating_Org_Area = BR_Parm.Rating_Org_Area  --2017/11/01修改為不分國內外
GROUP BY BR_Info.Reference_Nbr,
         BR_Info.Report_Date,
		 BR_Info.Bond_Type,
	     BR_Info.Rating_Type,
	     BR_Info.Rating_Object,
	     BR_Info.Rating_Org_Area,
		 BR_Info.Parm_ID,
		 BR_Info.Version,
		 BR_Info.Bond_Number,
		 BR_Info.Lots,
	     BR_Info.Portfolio,
	     BR_Info.Origination_Date,
	     BR_Info.Portfolio_Name,
	     BR_Info.SMF,
	     BR_Info.ISSUER,
		 BR_Parm.Rating_Selection,
		 BR_Parm.Rating_Priority,
         BR_Info.ISIN_Changed_Ind,
         BR_Info.Bond_Number_Old,
         BR_Info.Lots_Old,
         BR_Info.Portfolio_Name_Old,
         BR_Info.Origination_Date_Old
)
Insert Into Bond_Rating_Summary
            (
			  Reference_Nbr,
              Report_Date,
              Parm_ID,
              Bond_Type,
              Rating_Type,
              Rating_Object,
              Rating_Org_Area,
              Rating_Selection,
              Grade_Adjust,
              Rating_Priority,
              Processing_Date,
              Version,
              Bond_Number,
              Lots,
              Portfolio,
              Origination_Date,
              Portfolio_Name,
              SMF,
              ISSUER,
              ISIN_Changed_Ind,
              Bond_Number_Old,
              Lots_Old,
              Portfolio_Name_Old,
              Origination_Date_Old,
              Create_User,
              Create_Date,
              Create_Time
			)
select
			  Reference_Nbr,
              Report_Date,
              Parm_ID,
              Bond_Type,
              Rating_Type,
              Rating_Object,
              Rating_Org_Area,
              Rating_Selection,
              Grade_Adjust,
              Rating_Priority,
              Processing_Date,
              Version,
              Bond_Number,
              Lots,
              Portfolio,
              Origination_Date,
              Portfolio_Name,
              SMF,
              ISSUER,
              ISIN_Changed_Ind,
              Bond_Number_Old,
              Lots_Old,
              Portfolio_Name_Old,
              Origination_Date_Old,
              {_user.stringToStrSql()},
              {_date.dateTimeToStrSql()},
              {_time.timeSpanToStrSql()}
 from T4;
                        ";

                            #endregion A58 sql

                            #endregion
                            db.Database.CommandTimeout = 300;
                            //insert A57 原始日
                            db.Database.ExecuteSqlCommand(sql);
                            //insert A57 評估日
                            db.Database.ExecuteSqlCommand(sql1_2);
                            #region 共用
                            var A51s = db.Grade_Moody_Info.AsNoTracking()
                                         .Where(x => x.Status == "1").ToList();
                            var A52s = db.Grade_Mapping_Info.AsNoTracking()
                                         .Where(x => x.IsActive == "Y").ToList();
                            string Domestic = "國內";
                            string Foreign = "國外";
                            Grade_Moody_Info A51 = null;
                            #endregion
                            #region 新增3張特殊表單rating

                            var _ratingType = Rating_Type.B.GetDescription();
                            var _Bond_Ratings = db.Bond_Rating.AsNoTracking().ToList();
                            var _Issuer_Ratings = db.Issuer_Rating.AsNoTracking().ToList();
                            var _Guarantor_Ratings = db.Guarantor_Rating.AsNoTracking().ToList();
                            List<insertRating> _data = new List<insertRating>();
                            RatingObject _Rating_Object = RatingObject.Bonds;
                            string _RObject = string.Empty;
                            if (_Bond_Ratings.Any())
                            {
                                _Rating_Object = RatingObject.Bonds;
                                _RObject = _Rating_Object.GetDescription();
                                var _Bonds = _Bond_Ratings.Select(x => x.Bond_Number).ToList();
                                db.Bond_Rating_Info.AsNoTracking()
                                  .Where(x => x.Report_Date == dt &&
                                             x.Version == version &&
                                             x.Rating_Type == _ratingType &&
                                             _Bonds.Contains(x.Bond_Number)).ToList()
                                             .GroupBy(x => new { x.Reference_Nbr, x.Bond_Number })
                                  .ToList().ForEach(x =>
                                  {
                                      _data = new List<insertRating>();
                                      var _first = x.First();
                                      var _Bond_Rating = _Bond_Ratings.First(y => y.Bond_Number == x.Key.Bond_Number);
                                      if (!_Bond_Rating.S_And_P.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.SP.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.SP,
                                              _Rating = _Bond_Rating.S_And_P,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "RTG_SP"
                                          });
                                      }
                                      if (!_Bond_Rating.Moodys.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.Moody.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.Moody,
                                              _Rating = _Bond_Rating.Moodys,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "RTG_MOODY"
                                          });
                                      }
                                      if (!_Bond_Rating.Fitch.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.Fitch.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.Fitch,
                                              _Rating = _Bond_Rating.Fitch,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "RTG_FITCH"
                                          });
                                      }
                                      if (!_Bond_Rating.Fitch_TW.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.FitchTwn.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.FitchTwn,
                                              _Rating = _Bond_Rating.Fitch_TW,
                                              _RatingOrgArea = Domestic,
                                              _RTG_Bloomberg_Field = "RTG_FITCH_NATIONAL"
                                          });
                                      }
                                      if (!_Bond_Rating.TRC.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.CW.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.CW,
                                              _Rating = _Bond_Rating.TRC,
                                              _RatingOrgArea = Domestic,
                                              _RTG_Bloomberg_Field = "RTG_TRC"
                                          });
                                      }
                                      insertA57Rating(_data, _first, sql_A, _Rating_Object, A52s, A51s, parmIDs);
                                  });
                            }
                            if (_Issuer_Ratings.Any())
                            {
                                _Rating_Object = RatingObject.ISSUER;
                                _RObject = _Rating_Object.GetDescription();
                                var _Issuers = _Issuer_Ratings.Select(x => x.Issuer).ToList();
                                db.Bond_Rating_Info.AsNoTracking()
                                  .Where(x => x.Report_Date == dt &&
                                             x.Version == version &&
                                             x.Rating_Type == _ratingType &&
                                             _Issuers.Contains(x.ISSUER)).ToList()
                                             .GroupBy(x => new { x.Reference_Nbr, x.ISSUER })
                                  .ToList().ForEach(x =>
                                  {
                                      _data = new List<insertRating>();
                                      var _first = x.First();
                                      var _Issuer_Rating = _Issuer_Ratings.First(y => y.Issuer == x.Key.ISSUER);
                                      if (!_Issuer_Rating.S_And_P.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.SP.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.SP,
                                              _Rating = _Issuer_Rating.S_And_P,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "RTG_SP_LT_FC_ISSUER_CREDIT"
                                          });
                                      }
                                      if (!_Issuer_Rating.Moodys.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.Moody.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.Moody,
                                              _Rating = _Issuer_Rating.Moodys,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "RTG_MDY_ISSUER"
                                          });
                                      }
                                      if (!_Issuer_Rating.Fitch.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.Fitch.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.Fitch,
                                              _Rating = _Issuer_Rating.Fitch,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "RTG_FITCH_LT_ISSUER_DEFAULT"
                                          });
                                      }
                                      if (!_Issuer_Rating.Fitch_TW.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.FitchTwn.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.FitchTwn,
                                              _Rating = _Issuer_Rating.Fitch_TW,
                                              _RatingOrgArea = Domestic,
                                              _RTG_Bloomberg_Field = "RTG_FITCH_NATIONAL_LT"
                                          });
                                      }
                                      if (!_Issuer_Rating.TRC.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.CW.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.CW,
                                              _Rating = _Issuer_Rating.TRC,
                                              _RatingOrgArea = Domestic,
                                              _RTG_Bloomberg_Field = "RTG_TRC_LONG_TERM"
                                          });
                                      }
                                      insertA57Rating(_data, _first, sql_A, _Rating_Object, A52s, A51s,parmIDs);
                                  });
                            }
                            if (_Guarantor_Ratings.Any())
                            {
                                _Rating_Object = RatingObject.GUARANTOR;
                                _RObject = _Rating_Object.GetDescription();
                                var _Guarantors = _Guarantor_Ratings.Select(x => x.Issuer).ToList();
                                db.Bond_Rating_Info.AsNoTracking()
                                  .Where(x => x.Report_Date == dt &&
                                             x.Version == version &&
                                             x.Rating_Type == _ratingType &&
                                             _Guarantors.Contains(x.ISSUER)).ToList()
                                             .GroupBy(x => new { x.Reference_Nbr, x.ISSUER })
                                  .ToList().ForEach(x =>
                                  {
                                      _data = new List<insertRating>();
                                      var _first = x.First();
                                      var _Guarantor_Rating = _Guarantor_Ratings.First(y => y.Issuer == x.Key.ISSUER);
                                      if (!_Guarantor_Rating.S_And_P.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.SP.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.SP,
                                              _Rating = _Guarantor_Rating.S_And_P,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "G_RTG_SP_LT_FC_ISSUER_CREDIT"
                                          });
                                      }
                                      if (!_Guarantor_Rating.Moodys.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.Moody.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.Moody,
                                              _Rating = _Guarantor_Rating.Moodys,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "G_RTG_MDY_ISSUER"
                                          });
                                      }
                                      if (!_Guarantor_Rating.Fitch.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.Fitch.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.Fitch,
                                              _Rating = _Guarantor_Rating.Fitch,
                                              _RatingOrgArea = Foreign,
                                              _RTG_Bloomberg_Field = "G_RTG_FITCH_LT_ISSUER_DEFAULT"
                                          });
                                      }
                                      if (!_Guarantor_Rating.Fitch_TW.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.FitchTwn.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.FitchTwn,
                                              _Rating = _Guarantor_Rating.Fitch_TW,
                                              _RatingOrgArea = Domestic,
                                              _RTG_Bloomberg_Field = "G_RTG_FITCH_NATIONAL_LT"
                                          });
                                      }
                                      if (!_Guarantor_Rating.TRC.IsNullOrWhiteSpace() &&
                                          !x.Any(z =>
                                          z.Rating_Object == _RObject &&
                                          z.Rating_Org == RatingOrg.CW.GetDescription()))
                                      {
                                          _data.Add(new insertRating()
                                          {
                                              _Rating_Org = RatingOrg.CW,
                                              _Rating = _Guarantor_Rating.TRC,
                                              _RatingOrgArea = Domestic,
                                              _RTG_Bloomberg_Field = "G_RTG_TRC_LONG_TERM"
                                          });
                                      }

                                      insertA57Rating(_data, _first, sql_A, _Rating_Object, A52s, A51s, parmIDs);
                                  });
                            }
                            if (sql_A.Length > 0)
                                db.Database.ExecuteSqlCommand(sql_A.ToString());

                            #endregion 新增3張特殊表單rating
                            //刪除A57 預設
                            db.Database.ExecuteSqlCommand(sql_D);

                            #region issuer='GOV-TW-CEN' or 'GOV-Kaohsiung' or 'GOV-TAIPEI' then他們的債項評等放他們發行人的評等(PS:發行人的評等複製一份給債項評等)

                            var ISSUERstr = RatingObject.ISSUER.GetDescription();
                            var Bondstr = RatingObject.Bonds.GetDescription();
                            List<string> ISSUERs = new List<string>() { "GOV-TW-CEN", "GOV-Kaohsiung", "GOV-TAIPEI" };
                            StringBuilder sb2 = new StringBuilder();
                            var A57ISSUERs =
                                db.Bond_Rating_Info.AsNoTracking().Where(x =>
                                 x.Report_Date == dt &&
                                 x.Version == version &&
                                 x.Grade_Adjust != null &&
                                 x.PD_Grade != null &&
                                 ISSUERs.Contains(x.ISSUER) &&
                                 x.Rating_Object == ISSUERstr).ToList();
                            var A57Bonds =
                                 db.Bond_Rating_Info.AsNoTracking().Where(x =>
                                 x.Report_Date == dt &&
                                 x.Version == version &&
                                 ISSUERs.Contains(x.ISSUER) &&
                                 x.Rating_Object == Bondstr).ToList();
                            foreach (var A57group in A57ISSUERs.GroupBy(x => new
                            {
                                x.Reference_Nbr,
                                x.Bond_Number,
                                x.Lots,
                                x.Portfolio,
                                x.Segment_Name,
                                x.Bond_Type,
                                x.Lien_position,
                                x.Origination_Date,
                                x.Report_Date,
                                x.Rating_Type,
                                x.Rating_Org,
                                x.Rating_Object,
                                x.PD_Grade,
                                x.Grade_Adjust,
                                x.ISSUER_TICKER,
                                x.GUARANTOR_NAME,
                                x.GUARANTOR_EQY_TICKER,
                                x.Portfolio_Name,
                                x.SMF,
                                x.ISSUER,
                                x.Version,
                                x.Security_Ticker,
                                x.ISIN_Changed_Ind,
                                x.Bond_Number_Old,
                                x.Lots_Old,
                                x.Portfolio_Name_Old,
                                x.Origination_Date_Old
                            }))
                            {
                                var q = A57group;
                                var first = A57group.First();
                                var _D60Foreign = getParmID(parmIDs, first.Rating_Object, Foreign); //國外
                                var _D60Domestic = getParmID(parmIDs, first.Rating_Object, Domestic); //國內
                                var _D60 = new Bond_Rating_Parm();
                                if (_D60Foreign.Rating_Selection == _D60Domestic.Rating_Selection)
                                    _D60 = _D60Foreign;
                                else
                                    _D60 = _D60Foreign.Rating_Priority <= _D60Domestic.Rating_Priority ? _D60Foreign : _D60Domestic;
                                int? _PD_Grade = null;
                                if (_D60 != null)
                                {
                                    if (_D60.Rating_Selection == "1")
                                    {
                                        _PD_Grade = A57group.Where(z => z.PD_Grade != null).Min(z => z.PD_Grade);
                                    }
                                    if (_D60.Rating_Selection == "2")
                                    {
                                        _PD_Grade = A57group.Where(z => z.PD_Grade != null).Max(z => z.PD_Grade);
                                    }
                                }
                                if (_PD_Grade.HasValue)
                                {
                                    var _A57ISSUERcopy = A57group.First(z => z.PD_Grade == _PD_Grade.Value);
                                    var _A57Bond = A57Bonds.FirstOrDefault(z =>
                                                          z.Reference_Nbr == _A57ISSUERcopy.Reference_Nbr &&
                                                          z.Rating_Type == _A57ISSUERcopy.Rating_Type &&
                                                          z.Rating_Org == _A57ISSUERcopy.Rating_Org);
                                    if (_A57Bond == null) //沒有債項才須新增 (有債項就保持不變)
                                    {
                                        var RTGFiled = string.Empty;
                                        var RatingOrgArea = string.Empty;
                                        switch (_A57ISSUERcopy.Rating_Org)
                                        {
                                            case "SP":
                                                RTGFiled = "RTG_SP";
                                                RatingOrgArea = Foreign;
                                                break;

                                            case "Moody":
                                                RTGFiled = "RTG_MOODY";
                                                RatingOrgArea = Foreign;
                                                break;

                                            case "Fitch":
                                                RTGFiled = "RTG_FITCH";
                                                RatingOrgArea = Foreign;
                                                break;

                                            case "Fitch(twn)":
                                                RTGFiled = "RTG_FITCH_NATIONAL";
                                                RatingOrgArea = Domestic;
                                                break;

                                            case "CW":
                                                RTGFiled = "RTG_TRC";
                                                RatingOrgArea = Domestic;
                                                break;
                                        }
                                        if (!RTGFiled.IsNullOrWhiteSpace())
                                        {
                                            var _parm = getParmID(parmIDs, Bondstr, RatingOrgArea);
                                            _A57ISSUERcopy.Parm_ID = _parm?.Parm_ID.ToString();
                                            _A57ISSUERcopy.Rating_Object = Bondstr;
                                            _A57ISSUERcopy.Rating_Org_Area = RatingOrgArea;
                                            _A57ISSUERcopy.Fill_up_YN = null;
                                            _A57ISSUERcopy.Fill_up_Date = null;
                                            _A57ISSUERcopy.RTG_Bloomberg_Field = RTGFiled;
                                            sb2.Append(insertA57(_A57ISSUERcopy));
                                        }
                                    }
                                }
                            }
                            if (sb2.Length > 0)
                            {
                                db.Database.ExecuteSqlCommand(sb2.ToString());
                            }

                            #endregion issuer='GOV-TW-CEN' or 'GOV-Kaohsiung' or 'GOV-TAIPEI' then他們的債項評等放他們發行人的評等(PS:發行人的評等複製一份給債項評等)

                            //三張特殊表單更新
                            db.Database.ExecuteSqlCommand(sql1_3);

                            #region 複寫前一版本已補登之信評

                            if (complement == "Y")
                            {
                                StringBuilder sb = new StringBuilder();

                                List<Bond_Rating_Info> InsertA57 = new List<Bond_Rating_Info>();
                                List<Bond_Rating_Info> UpdateA57 = new List<Bond_Rating_Info>();

                                var _Rating_Type = Rating_Type.B.GetDescription();
                                var A57 = db.Bond_Rating_Info.AsNoTracking().Where(x => x.Report_Date == dt && x.Rating_Type == _Rating_Type);
                                var _ver = A57.Where(x => x.Version != version).Max(x => x.Version);
                                if (_ver != null)
                                {
                                    //目前版本
                                    var A57nAll = A57.Where(x => x.Version == version).ToList();  
                                    //目前版本
                                    //上一次最後一版
                                    var A57s = A57.Where(x => x.Version == _ver &&
                                                               x.Fill_up_YN == "Y").ToList();
                                    //上一次最後一版
                                    var datas = A57s
                                        .GroupBy(x => new
                                        {
                                            x.Reference_Nbr,
                                            x.Bond_Number,
                                            x.Lots,
                                            x.Rating_Type,
                                            x.Report_Date,
                                            x.Version,
                                            x.Portfolio_Name
                                        });
                                    foreach (var item in datas)
                                    {
                                        bool deleteSqlFlag = false;
                                        var deleteA57 = new Bond_Rating_Info();
                                        var first = item.First();
                                        //A57n => 這一版A57變更共用資料(新增A57用)
                                        var A57ns = A57nAll.Where(y =>
                                                        y.Bond_Number == first.Bond_Number &&
                                                        y.Lots == first.Lots &&
                                                        y.Portfolio_Name == first.Portfolio_Name).ToList();
                                        if (A57ns.Any()) //目前版本沒有比對到符合資料不需要繼續
                                        {
                                            var A57n = A57ns.First().ModelConvert<Bond_Rating_Info,Bond_Rating_Info>();
                                            //上一版有修改過的A57
                                            item.ToList().ForEach(
                                            x =>
                                            {
                                                A51 = null;
                                                if (x.Rating_Org == RatingOrg.Moody.GetDescription())
                                                {
                                                    A51 = A51s.FirstOrDefault(z => z.Rating == x.Rating);
                                                }
                                                else
                                                {
                                                    var A52 = A52s.FirstOrDefault(z => z.Rating_Org == x.Rating_Org &&
                                                                                       z.Rating == x.Rating);
                                                    if (A52 != null)
                                                        A51 = A51s.FirstOrDefault(z => z.PD_Grade == A52.PD_Grade);
                                                }
                                                    //找尋目前版本存不存在相同的資料
                                                    var A57o = new Bond_Rating_Info();
                                                A57o = A57ns.FirstOrDefault(y =>
                                                y.Rating_Object == x.Rating_Object &&
                                                y.RTG_Bloomberg_Field == x.RTG_Bloomberg_Field);
                                                    //不存在新增 防呆
                                                    if (A57o == null)
                                                {
                                                    var _parm = getParmID(parmIDs, x.Rating_Object, x.Rating_Org_Area);
                                                    A57n.Parm_ID = _parm?.Parm_ID.ToString();
                                                    A57n.Rating_Date = x.Rating_Date;
                                                    A57n.Rating_Object = x.Rating_Object;
                                                    A57n.Rating_Org = x.Rating_Org;
                                                    A57n.Rating = x.Rating;
                                                    A57n.Rating_Org_Area = x.Rating_Org_Area;
                                                    A57n.Fill_up_YN = "Y";
                                                    A57n.Fill_up_Date = startTime;
                                                    A57n.PD_Grade = A51?.PD_Grade;
                                                    A57n.Grade_Adjust = A51?.Grade_Adjust;
                                                    A57n.RTG_Bloomberg_Field = x.RTG_Bloomberg_Field;
                                                    sb.Append(insertA57(A57n));
                                                    if (A51 != null)
                                                        deleteSqlFlag = true;
                                                }
                                                    //存在修改
                                                    else if (A57o != null)
                                                {
                                                    sb.Append($@"
                                                    UPDATE [Bond_Rating_Info]
                                                       SET [Rating] = {x.Rating.stringToStrSql()}
                                                          ,[PD_Grade] = {(A51?.PD_Grade).intNToStrSql()}
                                                          ,[Grade_Adjust] = {(A51?.Grade_Adjust).intNToStrSql()}
                                                          ,[Rating_Date] = {x.Rating_Date.dateTimeNToStrSql()}
                                                          ,[Fill_up_YN] = 'Y'
                                                          ,[Fill_up_Date] = '{startTime.ToString("yyyy/MM/dd")}'
                                                    WHERE Reference_Nbr = { A57o.Reference_Nbr.stringToStrSql() }
                                                      AND Report_Date = { A57o.Report_Date.dateTimeToStrSql() }
                                                      AND Rating_Type = { A57o.Rating_Type.stringToStrSql() }
                                                      AND Bond_Number = { A57o.Bond_Number.stringToStrSql() }
                                                      AND Lots = { A57o.Lots.stringToStrSql() }
                                                      AND RTG_Bloomberg_Field = { A57o.RTG_Bloomberg_Field.stringToStrSql() }
                                                      AND Portfolio_Name = { A57o.Portfolio_Name.stringToStrSql() }
                                                      AND Version = { A57o.Version.intNToStrSql() }; ");
                                                }
                                            });
                                            if (deleteSqlFlag)
                                            {
                                                sb.Append($@"
                            Delete Bond_Rating_Info
                            where Reference_Nbr = {A57n.Reference_Nbr.stringToStrSql()}
                            and Bond_Number = {A57n.Bond_Number.stringToStrSql()}
                            and Lots = {A57n.Lots.stringToStrSql()}
                            and Rating_Type = {A57n.Rating_Type.stringToStrSql()}
                            and Report_Date = {A57n.Report_Date.dateTimeToStrSql()}
                            and Version = {A57n.Version.intNToStrSql()}
                            and Portfolio_Name = {A57n.Portfolio_Name.stringToStrSql()}
                            and Rating_Object = ' '
                            and Rating_Org is null
                            and Rating is null
                            and Rating_Org_Area = ' ' ;  ");
                                            }
                                        }
                                    }
                                    if (sb.Length > 0)
                                    {
                                        db.Database.ExecuteSqlCommand(sb.ToString());
                                    }
                                }
                            }

                            #endregion 複寫前一版本已補登之信評

                            //insert A58
                            db.Database.ExecuteSqlCommand(sql2);
                            dbContextTransaction.Commit();

                            DateTime endTime = DateTime.Now;

                            new BondsCheckRepository<Bond_Rating_Summary>(
                                db.Bond_Rating_Summary.AsNoTracking()
                                .Where(x => x.Report_Date == dt &&
                                x.Version == version).AsEnumerable(), 
                                Check_Table_Type.Bonds_A58_Transfer_Check,
                                dt,version);

                            db.Dispose();

                            log.bothLog(
                                TableType.A57.ToString(),
                                true,
                                dt,
                                startTime,
                                endTime,
                                version,
                                A57logPath,
                                MessageType.Success.GetDescription()
                                );
                            log.bothLog(
                                TableType.A58.ToString(),
                                true,
                                dt,
                                startTime,
                                endTime,
                                version,
                                A58logPath,
                                MessageType.Success.GetDescription()
                                );                            
                        }
                        catch (Exception ex)
                        {
                            //dbContextTransaction.Rollback(); //Required according to MSDN article 
                            log.bothLog(
                                TableType.A57.ToString(),
                                false,
                                dt,
                                startTime,
                                DateTime.Now,
                                version,
                                A57logPath,
                                $"message: {ex.Message}" +
                                $", inner message {ex.InnerException?.InnerException?.Message}"
                                );
                            log.bothLog(
                                TableType.A58.ToString(),
                                false,
                                dt,
                                startTime,
                                DateTime.Now,
                                version,
                                A58logPath,
                                $"message: {ex.Message}" +
                                $", inner message {ex.InnerException?.InnerException?.Message}"
                                );
                        }
                    }
                }
                else
                {
                    log.bothLog(
                        TableType.A57.ToString(),
                        false,
                        dt,
                        startTime,
                        DateTime.Now,
                        version,
                        A57logPath,
                        MessageType.not_Find_Any.GetDescription("A41")
                        );
                    log.bothLog(
                        TableType.A58.ToString(),
                        false,
                        dt,
                        startTime,
                        DateTime.Now,
                        version,
                        A58logPath,
                        MessageType.not_Find_Any.GetDescription("A41")
                        );
                }
            }
            else
            {
                log.bothLog(
                    TableType.A57.ToString(),
                    false,
                    dt,
                    startTime,
                    DateTime.Now,
                    version,
                    A57logPath,
                    MessageType.transferError.GetDescription()
                    );
                log.bothLog(
                    TableType.A58.ToString(),
                    false,
                    dt,
                    startTime,
                    DateTime.Now,
                    version,
                    A58logPath,
                    MessageType.transferError.GetDescription()
                    );
            }
        }

        #region
        /// <summary>
        /// C03 Mortgage save
        /// </summary>
        /// <param name="todayDate"></param>
        public void saveC03Mortgage(string todayDate)
        {
            string logPath = log.txtLocation(TableType.C03Mortgage.ToString());
            DateTime startTime = DateTime.Now;
            DateTime dt = DateTime.MinValue;
            DateTime.TryParseExact(todayDate, "yyyyMMdd", null,
                      System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dt);

            try
            {
                string thisYear = todayDate.Substring(0, 4);
                string lastYear = (int.Parse(thisYear) - 1).ToString();
                //string startYearQuartly = lastYear + "Q1";
                string startYearQuartly = "1995Q1";
                string endYearQuartly = thisYear + "Q4";

                IEnumerable<Loan_default_Info> A06Data = db.Loan_default_Info
                                                  .Where(x => x.Year_Quartly.CompareTo(startYearQuartly) >= 0
                                                              && x.Year_Quartly.CompareTo(endYearQuartly) <= 0
                                                        ).AsEnumerable();

                IEnumerable<Econ_Domestic> A07Data = db.Econ_Domestic
                                              .Where(x => x.Year_Quartly.CompareTo(startYearQuartly) >= 0
                                                          && x.Year_Quartly.CompareTo(endYearQuartly) <= 0
                                                    ).AsEnumerable();

                if (!A06Data.Any())
                {
                    #region 加入 sql transferCheck by Mark 2018/01/09
                    log.bothLog(
                        "C03",
                        false,
                        dt,
                        startTime,
                        DateTime.Now,
                        1,
                        logPath,
                        "Loan_default_Info 無 " + startYearQuartly + " ~ " + endYearQuartly + " 的資料"
                        );
                    #endregion
                }
                else if (!A07Data.Any())
                {
                    #region 加入 sql transferCheck by Mark 2018/01/09
                    log.bothLog(
                        "C03",
                        false,
                        dt,
                        startTime,
                        DateTime.Now,
                        1,
                        logPath,
                        "Econ_Domestic 無 " + startYearQuartly + " ~ " + endYearQuartly + " 的資料"
                        );
                    #endregion
                }
                else
                {
                    string productCode = GroupProductCode.M.GetDescription();
                    Group_Product_Code_Mapping gpcm = db.Group_Product_Code_Mapping.Where(x => x.Group_Product_Code.StartsWith(productCode)).FirstOrDefault();
                    if (gpcm == null)
                    {
                        #region 加入 sql transferCheck by Mark 2018/01/09
                        log.bothLog(
                            "C03",
                            false,
                            dt,
                            startTime,
                            DateTime.Now,
                            1,
                            logPath,
                            "Group_Product_Code_Mapping 無房貸的 Product_Code"
                            );
                        #endregion
                    }
                    else
                    {
                        productCode = gpcm.Product_Code;

                        var query = db.Econ_D_YYYYMMDD
                                    .Where(x => x.Year_Quartly.CompareTo(startYearQuartly) >= 0
                                                && x.Year_Quartly.CompareTo(endYearQuartly) <= 0
                                          ).ToList();
                        db.Econ_D_YYYYMMDD.RemoveRange(query);

                        string nowDate = DateTime.Now.ToString("yyyy/MM/dd");

                        var addData = (
                                             from a in A06Data
                                             join b in A07Data
                                             on new { a.Year_Quartly }
                                             equals new { b.Year_Quartly}
                                             select new Econ_D_YYYYMMDD()
                                             {
                                                 Processing_Date = nowDate,
                                                 Product_Code = productCode,
                                                 Data_ID = "",
                                                 Year_Quartly = b.Year_Quartly,
                                                 PD_Quartly = a.PD_Quartly,
                                                 TWSE_Index = Extension.doubleNToDouble(b.TWSE_Index),
                                                 TWRGSARP_Index = Extension.doubleNToDouble(b.TWRGSARP_Index),
                                                 TWGDPCON_Index = Extension.doubleNToDouble(b.TWGDPCON_Index),
                                                 TWLFADJ_Index = Extension.doubleNToDouble(b.TWLFADJ_Index),
                                                 TWCPI_Index = Extension.doubleNToDouble(b.TWCPI_Index),
                                                 TWMSA1A_Index = Extension.doubleNToDouble(b.TWMSA1A_Index),
                                                 TWMSA1B_Index = Extension.doubleNToDouble(b.TWMSA1B_Index),
                                                 TWMSAM2_Index = Extension.doubleNToDouble(b.TWMSAM2_Index),
                                                 GVTW10YR_Index = Extension.doubleNToDouble(b.GVTW10YR_Index),
                                                 TWTRBAL_Index = Extension.doubleNToDouble(b.TWTRBAL_Index),
                                                 TWTREXP_Index = Extension.doubleNToDouble(b.TWTREXP_Index),
                                                 TWTRIMP_Index = Extension.doubleNToDouble(b.TWTRIMP_Index),
                                                 TAREDSCD_Index = Extension.doubleNToDouble(b.TAREDSCD_Index),
                                                 TWCILI_Index = Extension.doubleNToDouble(b.TWCILI_Index),
                                                 TWBOPCUR_Index = Extension.doubleNToDouble(b.TWBOPCUR_Index),
                                                 EHCATW_Index = Extension.doubleNToDouble(b.EHCATW_Index),
                                                 TWINDPI_Index = Extension.doubleNToDouble(b.TWINDPI_Index),
                                                 TWWPI_Index = Extension.doubleNToDouble(b.TWWPI_Index),
                                                 TARSYOY_Index = Extension.doubleNToDouble(b.TARSYOY_Index),
                                                 TWEOTTL_Index = Extension.doubleNToDouble(b.TWEOTTL_Index),
                                                 SLDETIGT_Index = Extension.doubleNToDouble(b.SLDETIGT_Index),
                                                 TWIRFE_Index = Extension.doubleNToDouble(b.TWIRFE_Index),
                                                 SINYI_HOUSE_PRICE_index = Extension.doubleNToDouble(b.SINYI_HOUSE_PRICE_index),
                                                 CATHAY_ESTATE_index = Extension.doubleNToDouble(b.CATHAY_ESTATE_index),
                                                 Real_GDP2011 = Extension.doubleNToDouble(b.Real_GDP2011),
                                                 MCCCTW_Index = Extension.doubleNToDouble(b.MCCCTW_Index),
                                                 TRDR1T_Index = Extension.doubleNToDouble(b.TRDR1T_Index)
                                             }
                                      ).AsEnumerable();

                        db.Econ_D_YYYYMMDD.AddRange(addData);
                        db.SaveChanges();
                        #region 加入 sql transferCheck by Mark 2018/01/09
                        log.bothLog(
                            "C03",
                            true,
                            dt,
                            startTime,
                            DateTime.Now,
                            1,
                            logPath,
                            MessageType.Success.GetDescription()
                            );
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                #region 加入 sql transferCheck by Mark 2018/01/09
                log.bothLog(
                    "C03",
                    false,
                    dt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    ex.Message
                    );
                #endregion
            }
        }
        #endregion

        #region private function

        private string insertA57(Bond_Rating_Info A57)
        {
            return $@"
INSERT INTO [Bond_Rating_Info]
           ([Reference_Nbr]
           ,[Bond_Number]
           ,[Lots]
           ,[Portfolio]
           ,[Segment_Name]
           ,[Bond_Type]
           ,[Lien_position]
           ,[Origination_Date]
           ,[Report_Date]
           ,[Rating_Date]
           ,[Rating_Type]
           ,[Rating_Object]
           ,[Rating_Org]
           ,[Rating]
           ,[Rating_Org_Area]
           ,[Fill_up_YN]
           ,[Fill_up_Date]
           ,[PD_Grade]
           ,[Grade_Adjust]
           ,[ISSUER_TICKER]
           ,[GUARANTOR_NAME]
           ,[GUARANTOR_EQY_TICKER]
           ,[Parm_ID]
           ,[Portfolio_Name]
           ,[RTG_Bloomberg_Field]
           ,[SMF]
           ,[ISSUER]
           ,[Version]
           ,[Security_Ticker]
           ,[ISIN_Changed_Ind]
           ,[Bond_Number_Old]
           ,[Lots_Old]
           ,[Portfolio_Name_Old]
           ,[Origination_Date_Old]
           ,[Create_User]
           ,[Create_Date]
           ,[Create_Time]
)
     VALUES
           ({A57.Reference_Nbr.stringToStrSql()}
           ,{A57.Bond_Number.stringToStrSql()}
           ,{A57.Lots.stringToStrSql()}
           ,{A57.Portfolio.stringToStrSql()}
           ,{A57.Segment_Name.stringToStrSql()}
           ,{A57.Bond_Type.stringToStrSql()}
           ,{A57.Lien_position.stringToStrSql()}
           ,{A57.Origination_Date.dateTimeNToStrSql()}
           ,{A57.Report_Date.dateTimeToStrSql()}
           ,{A57.Rating_Date.dateTimeNToStrSql()}
           ,{A57.Rating_Type.stringToStrSql()}
           ,{A57.Rating_Object.stringToStrSql()}
           ,{A57.Rating_Org.stringToStrSql()}
           ,{A57.Rating.stringToStrSql()}
           ,{A57.Rating_Org_Area.stringToStrSql()}
           ,{A57.Fill_up_YN.stringToStrSql()}
           ,{A57.Fill_up_Date.dateTimeNToStrSql()}
           ,{A57.PD_Grade.intNToStrSql()}
           ,{A57.Grade_Adjust.intNToStrSql()}
           ,{A57.ISSUER_TICKER.stringToStrSql()}
           ,{A57.GUARANTOR_NAME.stringToStrSql()}
           ,{A57.GUARANTOR_EQY_TICKER.stringToStrSql()}
           ,{A57.Parm_ID.stringToStrSql()}
           ,{A57.Portfolio_Name.stringToStrSql()}
           ,{A57.RTG_Bloomberg_Field.stringToStrSql()}
           ,{A57.SMF.stringToStrSql()}
           ,{A57.ISSUER.stringToStrSql()}
           ,{A57.Version.intNToStrSql()}
           ,{A57.Security_Ticker.stringToStrSql()}
           ,{A57.ISIN_Changed_Ind.stringToStrSql()}
           ,{A57.Bond_Number_Old.stringToStrSql()}
           ,{A57.Lots_Old.stringToStrSql()}
           ,{A57.Portfolio_Name_Old.stringToStrSql()}
           ,{A57.Origination_Date_Old.dateTimeNToStrSql()}
           ,{_user.stringToStrSql()}
           ,{_date.dateTimeToStrSql()}
           ,{_time.timeSpanToStrSql()} ); ";
        }

        private void insertA57Rating(
            List<insertRating> _data,
            Bond_Rating_Info _first,
            StringBuilder sb,
            RatingObject _Rating_Object,
            List<Grade_Mapping_Info> A52s,
            List<Grade_Moody_Info> A51s,
            List<Bond_Rating_Parm> parmIDs)
        {
            foreach (var item in _data)
            {
                Bond_Rating_Info _copy = _first.ModelConvert<Bond_Rating_Info, Bond_Rating_Info>();
                Grade_Moody_Info A51 = null;
                if (item._Rating_Org == RatingOrg.Moody)
                {
                    A51 = A51s.FirstOrDefault(z => z.Rating == item._Rating);
                }
                else
                {
                    var A52 = A52s.FirstOrDefault(z => z.Rating_Org == item._Rating_Org.GetDescription() &&
                                                       z.Rating == item._Rating);
                    if (A52 != null)
                        A51 = A51s.FirstOrDefault(z => z.PD_Grade == A52.PD_Grade);
                }
                _copy.Rating_Object = _Rating_Object.GetDescription();
                _copy.Rating_Org_Area = item._RatingOrgArea;
                var _parm = getParmID(parmIDs, _copy.Rating_Object, _copy.Rating_Org_Area);
                _copy.Rating = item._Rating;
                _copy.Rating_Org = item._Rating_Org.GetDescription();
                _copy.RTG_Bloomberg_Field = item._RTG_Bloomberg_Field;
                _copy.Parm_ID = _parm?.Parm_ID.ToString();
                _copy.Fill_up_YN = null;
                _copy.Fill_up_Date = null;
                _copy.PD_Grade = A51?.PD_Grade;
                _copy.Grade_Adjust = A51?.Grade_Adjust;
                sb.Append(insertA57(_copy));
            }
        }

        /// <summary>
        /// get ParmIDs
        /// </summary>
        /// <returns></returns>
        private List<Bond_Rating_Parm> getParmIDs()
        {
            List<Bond_Rating_Parm> parmIDs = new List<Bond_Rating_Parm>();
            using (IFRS9DBEntities db = new IFRS9DBEntities())
            {
                var parms = db.Bond_Rating_Parm.AsNoTracking()
                    .Where(j => j.IsActive == "Y" && j.Status == "2").ToList(); //抓取所有有效資料
                foreach (var item in parms.GroupBy(x => new { x.Rating_Object, x.Rating_Org_Area }))
                {
                    if (item.Any(y => y.Rating_Org_Area == null) && item.Count() >= 2) //國內/外類別 兩種設定(1.單筆null,2.一筆國內一筆國外)
                        return parmIDs;
                }
                parmIDs = parms;
            }
            return parmIDs;
        }

        /// <summary>
        /// get ParmID
        /// </summary>
        /// <returns></returns>
        private Bond_Rating_Parm getParmID(List<Bond_Rating_Parm> datas, string Rating_Object,string Rating_Org_Area)
        {
            Bond_Rating_Parm _parm = datas.FirstOrDefault(m => m.Rating_Org_Area != null && m.Rating_Object == Rating_Object && m.Rating_Org_Area == Rating_Org_Area);
            if (_parm == null)
                _parm = datas.FirstOrDefault(m => m.Rating_Object == Rating_Object);
            return _parm;
        }

        private class insertRating
        {
            public RatingOrg _Rating_Org { get; set; }
            public string _Rating { get; set; }
            public string _RatingOrgArea { get; set; }
            public string _RTG_Bloomberg_Field { get; set; }
        }

        #endregion
    }
}