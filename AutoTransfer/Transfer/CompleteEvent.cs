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
        private IFRS9Entities db = new IFRS9Entities();
        private Log log = new Log();
        private string A57logPath = string.Empty;
        private string A58logPath = string.Empty;

        /// <summary>
        /// A53 save 後續動作(save A57,A58)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="version"></param>
        public void saveDb(DateTime dt, int version)
        {
            A57logPath = log.txtLocation(TableType.A57.ToString());
            A58logPath = log.txtLocation(TableType.A58.ToString());

            DateTime startTime = DateTime.Now;

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
                            string parmID = getParmID(); //選取離今日最近的D60
                            string reportData = dt.ToString("yyyy/MM/dd");
                            string ver = version.ToString();
                            string complement = ConfigurationManager.AppSettings["complement"];
                            string sql = string.Empty;
                            string sql2 = string.Empty;
                            sql = $@"
--原始

WITH temp2 AS
(
  SELECT
  CASE WHEN ((SELECT TOP 1 Report_Date FROM Bond_Rating_Info WHERE Report_Date = '{reportData}') != null)
       THEN '{reportData}'
	   ELSE (SELECT TOP 1 Report_Date from Bond_Rating_Info order by Report_Date desc)
  END AS Report_Date
), --最後一版A57
temp AS
(
select RTG_Bloomberg_Field,
       Rating_Object,
       CASE WHEN ISIN_Changed_Ind = 'Y'
            THEN Bond_Number_Old
            ELSE Bond_Number
       END  AS Bond_Number,
       CASE WHEN ISIN_Changed_Ind = 'Y'
            THEN Lots_Old
            ELSE Lots
       END  AS Lots,
       CASE WHEN ISIN_Changed_Ind = 'Y'
            THEN Portfolio_Name_Old
            ELSE Portfolio_Name
       END  AS Portfolio_Name,
       Rating,
       Rating_Date,
       Rating_Org,
       A57.Report_Date
FROM   Bond_Rating_Info A57,temp2
WHERE  A57.Rating_Type = '{Rating_Type.A.GetDescription()}'
AND    A57.Version = (select top 1 Version from Bond_Rating_Info where Report_Date = temp2.Report_Date order by Version desc)
AND    A57.Report_Date = temp2.Report_Date
AND    A57.Grade_Adjust is not null
), --最後一版A57(原始投資信評)
A52 AS (
   SELECT * FROM Grade_Mapping_Info
),
A51 AS (
   SELECT * FROM Grade_Moody_Info
   WHERE Effective = 'Y'
),
A41 AS(
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
   and   Version =  {ver}) AS A41
   LEFT JOIN Rating_Info_SampleInfo _A53Sample
   ON  A41.Bond_Number = _A53Sample.Bond_Number
   AND _A53Sample.Report_Date = '{reportData}'
), --全部的A41
tempA57t AS (
SELECT _A57.*,
       _A51.Grade_Adjust,
       _A51.PD_Grade
FROM   temp _A57
left Join   A52 _A52
ON     _A57.Rating_Org <> '{RatingOrg.Moody.GetDescription()}'
AND    _A57.Rating = _A52.Rating
AND    _A57.Rating_Org = _A52.Rating_Org
left JOIN   A51 _A51
ON     _A52.PD_Grade = _A51.PD_Grade
WHERE  _A57.Rating_Org <> '{RatingOrg.Moody.GetDescription()}'
  UNION ALL
SELECT _A57.*,
       _A51_2.Grade_Adjust,
       _A51_2.PD_Grade
FROM   temp _A57
left JOIN   A51 _A51_2
ON     _A57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
AND    _A57.Rating = _A51_2.Rating
WHERE  _A57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
), --最後一版A57(原始投資信評) 加入信評
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
   FROM  A41 BA_Info --A41
   JOIN  tempA57t oldA57 --oldA57
   ON    BA_Info.Bond_Number =  oldA57.Bond_Number
   AND   BA_Info.Lots = oldA57.Lots
   AND   BA_Info.Portfolio_Name = oldA57.Portfolio_Name
   AND   BA_Info.ISIN_Changed_Ind is null
   WHERE BA_Info.ISIN_Changed_Ind is null --沒換券的A41
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
   FROM  A41 BA_Info --A41
   JOIN  tempA57t oldA57 --oldA57 
   ON    BA_Info.Bond_Number_Old = oldA57.Bond_Number
   AND   BA_Info.Lots_Old = oldA57.Lots
   AND   BA_Info.Portfolio_Name_Old = oldA57.Portfolio_Name 
   AND   BA_Info.ISIN_Changed_Ind = 'Y'
   WHERE BA_Info.ISIN_Changed_Ind = 'Y' --換券的A41
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
from  A41 BA_Info 
),
T1all AS(
  select * from T1
  UNION ALL
  select * from T1s
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
           Origination_Date_Old)
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
           '{parmID}',
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
           Origination_Date_Old
		   From
		   T1all ; 

with tempDele as
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
and Reference_Nbr in (select Reference_Nbr from tempDele);

--評估日

WITH A52 AS (
   SELECT * FROM Grade_Mapping_Info
),
A51 AS (
   SELECT * FROM Grade_Moody_Info
   WHERE Effective = 'Y'
),
A41 AS (
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
	 FROM Bond_Account_Info A41
	 LEFT JOIN Rating_Info_SampleInfo _A53Sample
	 ON   A41.Bond_Number = _A53Sample.Bond_Number
     AND  _A53Sample.Report_Date = '{reportData}'
     AND  A41.Report_Date = '{reportData}'
     AND  A41.VERSION = {ver}
     WHERE A41.Report_Date = '{reportData}'
     AND  A41.VERSION = {ver}
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
   FROM  A41 BA_Info --A41
   LEFT JOIN A53t RA_Info --A53
   ON BA_Info.Bond_Number = RA_Info.Bond_Number
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
           Origination_Date_Old)
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
           '{Rating_Type.B.GetDescription()}',
           Rating_Object,
           Rating_Org,
           Rating,
           Rating_Org_Area,
           PD_Grade,
           Grade_Adjust,
           ISSUER_TICKER,
           GUARANTOR_NAME,
           GUARANTOR_EQY_TICKER,
           '{parmID}',
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
           Origination_Date_Old
		   From
		   T0;

-- update Bond_Rating(債項信評) 的設定
update Bond_Rating_Info   
set Rating = 
      case
	    when Bond_Rating_Info.Rating_Org = '{RatingOrg.SP.GetDescription()}' and Bond_Rating.S_And_P is not null
	     then Bond_Rating.S_And_P
        when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}' and Bond_Rating.Moodys is not null
	     then Bond_Rating.Moodys
		when Bond_Rating_Info.Rating_Org = '{RatingOrg.Fitch.GetDescription()}' and Bond_Rating.Fitch is not null
		 then Bond_Rating.Fitch
		when Bond_Rating_Info.Rating_Org = '{RatingOrg.FitchTwn.GetDescription()}' and Bond_Rating.Fitch_TW is not null
		 then Bond_Rating.Fitch_TW
		when  Bond_Rating_Info.Rating_Org = '{RatingOrg.CW.GetDescription()}' and Bond_Rating.TRC is not null
		 then Bond_Rating.TRC
	  else Bond_Rating_Info.Rating
	  end
from Bond_Rating
where  Bond_Rating_Info.Bond_Number = Bond_Rating.Bond_Number
and Bond_Rating_Info.Report_Date = '{reportData}'
and Bond_Rating_Info.Version = {ver}
and Bond_Rating_Info.Rating_Object = '{RatingObject.Bonds.GetDescription()}' ;

update Bond_Rating_Info
set PD_Grade = 
       case
         when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
         then (select top 1 PD_Grade from Grade_Moody_Info A51 where A51.Effective = 'Y' and A51.Rating = Bond_Rating_Info.Rating)
	     else (select top 1 PD_Grade from
	       Grade_Moody_Info A51 
		   where A51.Effective = 'Y' and A51.PD_Grade =
	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating))
       end ,
    Grade_Adjust =  
         case 
	     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
		 then (select top 1 Grade_Adjust from Grade_Moody_Info A51 where A51.Effective = 'Y' and A51.Rating = Bond_Rating_Info.Rating)
		 else (select top 1 Grade_Adjust from
	       Grade_Moody_Info A51 
		   where A51.Effective = 'Y' and A51.PD_Grade =
	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating))
      end
from Bond_Rating
where  Bond_Rating_Info.Bond_Number = Bond_Rating.Bond_Number
and Bond_Rating_Info.Report_Date = '{reportData}'
and Bond_Rating_Info.Version = {ver}
and Bond_Rating_Info.Rating_Object = '{RatingObject.Bonds.GetDescription()}' ;

-- update Issuer_Rating(發行者信評) 的設定
update Bond_Rating_Info   
set Rating = 
      case
	    when Bond_Rating_Info.Rating_Org = '{RatingOrg.SP.GetDescription()}' and Issuer_Rating.S_And_P is not null
	     then Issuer_Rating.S_And_P
        when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}' and Issuer_Rating.Moodys is not null
	     then Issuer_Rating.Moodys
		when Bond_Rating_Info.Rating_Org = '{RatingOrg.Fitch.GetDescription()}' and Issuer_Rating.Fitch is not null
		 then Issuer_Rating.Fitch
		when Bond_Rating_Info.Rating_Org = '{RatingOrg.FitchTwn.GetDescription()}' and Issuer_Rating.Fitch_TW is not null
		 then Issuer_Rating.Fitch_TW
		when  Bond_Rating_Info.Rating_Org = '{RatingOrg.CW.GetDescription()}' and Issuer_Rating.TRC is not null
		 then Issuer_Rating.TRC
	  else Bond_Rating_Info.Rating
	  end
from Issuer_Rating
where  Bond_Rating_Info.ISSUER = Issuer_Rating.Issuer
and Bond_Rating_Info.Report_Date = '{reportData}'
and Bond_Rating_Info.Version = {ver}
and Bond_Rating_Info.Rating_Object = '{RatingObject.ISSUER.GetDescription()}' ;

update Bond_Rating_Info
set PD_Grade = 
       case
         when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
         then (select top 1 PD_Grade from Grade_Moody_Info A51 where A51.Effective = 'Y' and A51.Rating = Bond_Rating_Info.Rating)
	     else (select top 1 PD_Grade from
	       Grade_Moody_Info A51 
		   where A51.Effective = 'Y' and A51.PD_Grade =
	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating))
       end ,
    Grade_Adjust =  
         case 
	     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
		 then (select top 1 Grade_Adjust from Grade_Moody_Info A51 where A51.Effective = 'Y' and A51.Rating = Bond_Rating_Info.Rating)
		 else (select top 1 Grade_Adjust from
	       Grade_Moody_Info A51 
		   where A51.Effective = 'Y' and A51.PD_Grade =
	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating))
      end
from Issuer_Rating
where  Bond_Rating_Info.ISSUER = Issuer_Rating.Issuer
and Bond_Rating_Info.Report_Date = '{reportData}'
and Bond_Rating_Info.Version = {ver}
and Bond_Rating_Info.Rating_Object = '{RatingObject.ISSUER.GetDescription()}' ;

-- update Guarantor_Rating(擔保者信評) 的設定
update Bond_Rating_Info   
set Rating = 
      case
	    when Bond_Rating_Info.Rating_Org = '{RatingOrg.SP.GetDescription()}' and Guarantor_Rating.S_And_P is not null
	     then Guarantor_Rating.S_And_P
        when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}' and Guarantor_Rating.Moodys is not null
	     then Guarantor_Rating.Moodys
		when Bond_Rating_Info.Rating_Org = '{RatingOrg.Fitch.GetDescription()}' and Guarantor_Rating.Fitch is not null
		 then Guarantor_Rating.Fitch
		when Bond_Rating_Info.Rating_Org = '{RatingOrg.FitchTwn.GetDescription()}' and Guarantor_Rating.Fitch_TW is not null
		 then Guarantor_Rating.Fitch_TW
		when  Bond_Rating_Info.Rating_Org = '{RatingOrg.CW.GetDescription()}' and Guarantor_Rating.TRC is not null
		 then Guarantor_Rating.TRC
	  else Bond_Rating_Info.Rating
	  end
from Guarantor_Rating
where  Bond_Rating_Info.ISSUER = Guarantor_Rating.Issuer
and Bond_Rating_Info.Report_Date = '{reportData}'
and Bond_Rating_Info.Version = {ver}
and Bond_Rating_Info.Rating_Object = '{RatingObject.GUARANTOR.GetDescription()}' ;

update Bond_Rating_Info
set PD_Grade = 
       case
         when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
         then (select top 1 PD_Grade from Grade_Moody_Info A51 where A51.Effective = 'Y' and A51.Rating = Bond_Rating_Info.Rating)
	     else (select top 1 PD_Grade from
	       Grade_Moody_Info A51 
		   where A51.Effective = 'Y' and A51.PD_Grade =
	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating))
       end ,
    Grade_Adjust =  
         case 
	     when Bond_Rating_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
		 then (select top 1 Grade_Adjust from Grade_Moody_Info A51 where A51.Effective = 'Y' and A51.Rating = Bond_Rating_Info.Rating)
		 else (select top 1 Grade_Adjust from
	       Grade_Moody_Info A51 
		   where A51.Effective = 'Y' and A51.PD_Grade =
	    (select top 1 PD_Grade from Grade_Mapping_Info A52 where A52.Rating_Org = Bond_Rating_Info.Rating_Org and A52.Rating = Bond_Rating_Info.Rating))
      end
from Guarantor_Rating
where  Bond_Rating_Info.ISSUER = Guarantor_Rating.Issuer
and Bond_Rating_Info.Report_Date = '{reportData}'
and Bond_Rating_Info.Version = {ver}
and Bond_Rating_Info.Rating_Object = '{RatingObject.GUARANTOR.GetDescription()}' ;

";

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
LEFT JOIN  Bond_Rating_Parm BR_Parm
on         BR_Info.Parm_ID = BR_Parm.Parm_ID
AND        BR_Info.Rating_Object = BR_Parm.Rating_Object
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
              Origination_Date_Old
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
              Origination_Date_Old			
 from T4;
                            ";
                            db.Database.CommandTimeout = 300;
                            db.Database.ExecuteSqlCommand(sql);
                            #region issuer='GOV-TW-CEN' or 'GOV-Kaohsiung' or 'GOV-TAIPEI' then他們的債項評等放他們發行人的評等
                            var ISSUERstr = RatingObject.ISSUER.GetDescription();
                            var Bondstr = RatingObject.Bonds.GetDescription();
                            List<string> ISSUERs = new List<string>() { "GOV-TW-CEN", "GOV-Kaohsiung", "GOV-TAIPEI" };
                            StringBuilder sb2 = new StringBuilder();
                            var D60 = db.Bond_Rating_Parm.AsNoTracking().ToList();
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
                            string Domestic = "國內";
                            string Foreign = "國外";
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
                                x.Parm_ID,
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
                                var _D60 = D60.FirstOrDefault(z => z.Parm_ID == first.Parm_ID &&
                                                            z.Rating_Object == first.Rating_Object);
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
                                            sb2.Append($@"
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
           ,[Portfolio_Name_Old])
     VALUES
           ({_A57ISSUERcopy.Reference_Nbr.stringToStrSql()}
           ,{_A57ISSUERcopy.Bond_Number.stringToStrSql()}
           ,{_A57ISSUERcopy.Lots.stringToStrSql()}
           ,{_A57ISSUERcopy.Portfolio.stringToStrSql()}
           ,{_A57ISSUERcopy.Segment_Name.stringToStrSql()}
           ,{_A57ISSUERcopy.Bond_Type.stringToStrSql()}
           ,{_A57ISSUERcopy.Lien_position.stringToStrSql()}
           ,{_A57ISSUERcopy.Origination_Date.dateTimeNToStrSql()}
           ,{_A57ISSUERcopy.Report_Date.dateTimeToStrSql()}
           ,{_A57ISSUERcopy.Rating_Date.dateTimeNToStrSql()}
           ,{_A57ISSUERcopy.Rating_Type.stringToStrSql()}
           ,{Bondstr.stringToStrSql()}
           ,{_A57ISSUERcopy.Rating_Org.stringToStrSql()}
           ,{_A57ISSUERcopy.Rating.stringToStrSql()}
           ,{RatingOrgArea.stringToStrSql()}
           ,null
           ,null
           ,{_A57ISSUERcopy.PD_Grade.intNToStrSql()}
           ,{_A57ISSUERcopy.Grade_Adjust.intNToStrSql()}
           ,{_A57ISSUERcopy.ISSUER_TICKER.stringToStrSql()}
           ,{_A57ISSUERcopy.GUARANTOR_NAME.stringToStrSql()}
           ,{_A57ISSUERcopy.GUARANTOR_EQY_TICKER.stringToStrSql()}
           ,{_A57ISSUERcopy.Parm_ID.stringToStrSql()}
           ,{_A57ISSUERcopy.Portfolio_Name.stringToStrSql()}
           ,{RTGFiled.stringToStrSql()}
           ,{_A57ISSUERcopy.SMF.stringToStrSql()}
           ,{_A57ISSUERcopy.ISSUER.stringToStrSql()}
           ,{_A57ISSUERcopy.Version.intNToStrSql()}
           ,{_A57ISSUERcopy.Security_Ticker.stringToStrSql()}
           ,{_A57ISSUERcopy.ISIN_Changed_Ind.stringToStrSql()}
           ,{_A57ISSUERcopy.Bond_Number_Old.stringToStrSql()}
           ,{_A57ISSUERcopy.Lots_Old.stringToStrSql()}
           ,{_A57ISSUERcopy.Portfolio_Name_Old.stringToStrSql()} );
                    ");
                                        }
                                    }
                                }
                            }
                            if (sb2.Length > 0)
                            {
                                db.Database.ExecuteSqlCommand(sb2.ToString());
                            }
                            #endregion
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
                                    var A51s = db.Grade_Moody_Info.AsNoTracking()
                                                 .Where(x => x.Effective == "Y").ToList();
                                    var A52s = db.Grade_Mapping_Info.AsNoTracking().ToList();
                                    var D60s = db.Bond_Rating_Parm.AsNoTracking().ToList();
                                    var A57n1 = A57.Where(x => x.Version == version && x.ISIN_Changed_Ind == "Y").ToList();
                                    var A57n2 = A57.Where(x => x.Version == version && x.ISIN_Changed_Ind == null).ToList();
                                    var A57s1 = A57.Where(x => x.Version == _ver).ToList();
                                    var A57s2 = A57s1.Where(x => x.Fill_up_YN == "Y" &&
                                                             x.Grade_Adjust != null).ToList();
                                    var datas = A57s2
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
                                        var _Bond_Number = string.Empty;
                                        var _Lots = string.Empty;
                                        var _Portfolio_Name = string.Empty;
                                        var first = item.First();
                                        if (first.ISIN_Changed_Ind == "Y")
                                        {
                                            _Bond_Number = first.Bond_Number_Old;
                                            _Lots = first.Lots_Old;
                                            _Portfolio_Name = first.Portfolio_Name_Old;
                                        }
                                        else
                                        {
                                            _Bond_Number = first.Bond_Number;
                                            _Lots = first.Lots;
                                            _Portfolio_Name = first.Portfolio_Name;
                                        }
                                        //A57n => 這一版A57變更共用資料(新增A57用)
                                        var A57n = A57n2.FirstOrDefault(y =>
                                                        y.Bond_Number == _Bond_Number &&
                                                        y.Lots == _Lots &&
                                                        y.Portfolio_Name == _Portfolio_Name
                                        );
                                        if (A57n == null)
                                            A57n = A57n1.FirstOrDefault(y =>
                                                       y.Bond_Number_Old == _Bond_Number &&
                                                       y.Lots_Old == _Lots &&
                                                       y.Portfolio_Name_Old == _Portfolio_Name
                                        );
                                        item.ToList().ForEach(
                                            x =>
                                            {
                                                Grade_Moody_Info A51 = null;
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
                                                A57o = A57n1.FirstOrDefault(y =>
                                                y.Rating_Object == x.Rating_Object &&
                                                y.RTG_Bloomberg_Field == x.RTG_Bloomberg_Field &&
                                                y.Bond_Number_Old == _Bond_Number &&
                                                y.Lots_Old == _Lots &&
                                                y.Portfolio_Name_Old == _Portfolio_Name);
                                                if (A57o == null)
                                                    A57o = A57n2.FirstOrDefault(y =>
                                                y.Rating_Object == x.Rating_Object &&
                                                y.RTG_Bloomberg_Field == x.RTG_Bloomberg_Field &&
                                                y.Bond_Number == _Bond_Number &&
                                                y.Lots == _Lots &&
                                                y.Portfolio_Name == _Portfolio_Name);
                                                    //不存在新增
                                                    if (A57o == null)
                                                {
                                                    sb.Append(
                                                    $@"
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
                                           ,[Portfolio_Name_Old])
                                     VALUES
                                           ({A57n.Reference_Nbr.stringToStrSql()}
                                           ,{A57n.Bond_Number.stringToStrSql()}
                                           ,{A57n.Lots.stringToStrSql()}
                                           ,{A57n.Portfolio.stringToStrSql()}
                                           ,{A57n.Segment_Name.stringToStrSql()}
                                           ,{A57n.Bond_Type.stringToStrSql()}
                                           ,{A57n.Lien_position.stringToStrSql()}
                                           ,{A57n.Origination_Date.dateTimeNToStrSql()}
                                           ,{A57n.Report_Date.dateTimeToStrSql()}
                                           ,{x.Rating_Date.dateTimeNToStrSql()}
                                           ,{A57n.Rating_Type.stringToStrSql()}
                                           ,{x.Rating_Object.stringToStrSql()}
                                           ,{x.Rating_Org.stringToStrSql()}
                                           ,{x.Rating.stringToStrSql()}
                                           ,{x.Rating_Org_Area.stringToStrSql()}
                                           ,'Y'
                                           ,'{startTime.ToString("yyyy/MM/dd")}'
                                           ,{A51.PD_Grade.intNToStrSql()}
                                           ,{A51.Grade_Adjust.intNToStrSql()}
                                           ,{A57n.ISSUER_TICKER.stringToStrSql()}
                                           ,{A57n.GUARANTOR_NAME.stringToStrSql()}
                                           ,{A57n.GUARANTOR_EQY_TICKER.stringToStrSql()}
                                           ,{A57n.Parm_ID.stringToStrSql()}
                                           ,{A57n.Portfolio_Name.stringToStrSql()}
                                           ,{x.RTG_Bloomberg_Field.stringToStrSql()}
                                           ,{A57n.SMF.stringToStrSql()}
                                           ,{A57n.ISSUER.stringToStrSql()}
                                           ,{A57n.Version.intNToStrSql()}
                                           ,{A57n.Security_Ticker.stringToStrSql()}
                                           ,{A57n.ISIN_Changed_Ind.stringToStrSql()}
                                           ,{A57n.Bond_Number_Old.stringToStrSql()}
                                           ,{A57n.Lots_Old.stringToStrSql()}
                                           ,{A57n.Portfolio_Name_Old.stringToStrSql()} ); ");
                                                    deleteSqlFlag = true;
                                                }
                                                    //存在修改
                                                    else
                                                {
                                                    sb.Append($@"
                                UPDATE [Bond_Rating_Info]
                                   SET [Rating] = {x.Rating.stringToStrSql()}
                                      ,[PD_Grade] = {A51.PD_Grade.intNToStrSql()}
                                      ,[Grade_Adjust] = {A51.Grade_Adjust.intNToStrSql()} 
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
                                    if (sb.Length > 0)
                                        db.Database.ExecuteSqlCommand(sb.ToString());
                                }
                            }
                            #endregion
                            db.Database.ExecuteSqlCommand(sql2);
                            dbContextTransaction.Commit();
                            db.Dispose();
                            log.bothLog(
                                TableType.A57.ToString(),
                                true,
                                dt,
                                startTime,
                                DateTime.Now,
                                version,
                                A57logPath,
                                MessageType.Success.GetDescription()
                                );
                            log.bothLog(
                                TableType.A58.ToString(),
                                true,
                                dt,
                                startTime,
                                DateTime.Now,
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
                string startYearQuartly = lastYear + "Q1";
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
        /// <summary>
        /// 判斷國內外
        /// </summary>
        /// <param name="ratingOrg"></param>
        /// <returns></returns>
        private string formatOrgArea(string ratingOrg)
        {
            if (ratingOrg.Equals(RatingOrg.SP.GetDescription()) ||
               ratingOrg.Equals(RatingOrg.Moody.GetDescription()) ||
               ratingOrg.Equals(RatingOrg.Fitch.GetDescription()))
                return "國外";
            if (ratingOrg.Equals(RatingOrg.FitchTwn.GetDescription()) ||
                ratingOrg.Equals(RatingOrg.CW.GetDescription()))
                return "國內";
            return string.Empty;
        }

        /// <summary>
        /// get Security_Ticker
        /// </summary>
        /// <param name="SMF"></param>
        /// <param name="bondNumber"></param>
        /// <returns></returns>
        private string getSecurityTicker(string SMF, string bondNumber)
        {
            List<string> Mtges = new List<string>() { "A11", "932" };
            if (!SMF.IsNullOrWhiteSpace() && SMF.Trim().Length > 3)
                if (Mtges.Contains(SMF.Substring(0, 3)))
                    return string.Format("{0} {1}", bondNumber, "Mtge");
            return string.Format("{0} {1}", bondNumber, "Corp");
        }

        /// <summary>
        /// get ParmID
        /// </summary>
        /// <returns></returns>
        private string getParmID()
        {
            string parmID = string.Empty;
            DateTime dt = DateTime.Now;
            var parms = db.Bond_Rating_Parm
            .Where(j => j.Processing_Date != null &&
                   dt > j.Processing_Date);
            Bond_Rating_Parm parmf =
            parms.FirstOrDefault(q => q.Processing_Date == parms.Max(w => w.Processing_Date));
            if (parmf != null) // 參數編號
                parmID = parmf.Parm_ID;
            return parmID;
        }

        /// <summary>
        /// 抓取 sampleInfo
        /// </summary>
        /// <param name="sampleInfos"></param>
        /// <param name="info"></param>
        /// <param name="nullarr"></param>
        /// <returns></returns>
        private sampleInfo formateSampleInfo(
            List<A53.sampleInfo> sampleInfos,
            Rating_Info info,
            List<string> nullarr)
        {
            if (RatingObject.Bonds.GetDescription().Equals(info.Rating_Object))
            {
                sampleInfo s = sampleInfos.FirstOrDefault(j => info.Bond_Number.Equals(j.Bond_Number));
                if (s != null)
                {
                    return new sampleInfo()
                    {
                        Bond_Number = s.Bond_Number,
                        ISSUER_TICKER = sampleInfoValue(s.ISSUER_TICKER, nullarr),
                        GUARANTOR_EQY_TICKER = sampleInfoValue(s.GUARANTOR_EQY_TICKER, nullarr),
                        GUARANTOR_NAME = sampleInfoValue(s.GUARANTOR_NAME, nullarr)
                    };
                }
            }
            return new sampleInfo();
        }

        /// <summary>
        /// 判斷sampleInfo 參數
        /// </summary>
        /// <param name="value"></param>
        /// <param name="nullarr"></param>
        /// <returns></returns>
        private string sampleInfoValue(string value, List<string> nullarr)
        {
            if (value == null)
                return null;
            if (nullarr.Contains(value.Trim()))
                return null;
            return value.Trim();
        }

        #endregion
    }
}