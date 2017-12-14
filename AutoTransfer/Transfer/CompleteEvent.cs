using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    try
                    {
                            string parmID = getParmID(); //選取離今日最近的D60
                            string reportData = dt.ToString("yyyy/MM/dd");
                            string ver = version.ToString();
                            string sql = string.Empty;

                        sql = $@"
with temp2 as
(
  select
  CASE WHEN ((select TOP 1 Report_Date from Bond_Rating_Info where Report_Date = '{reportData}') != null)
       THEN (select TOP 1 Report_Date from Bond_Rating_Info where Report_Date = '{reportData}')
	   ELSE MAX(Report_Date) 
  END as Report_Date from Bond_Rating_Info 
),
temp as
(
select RTG_Bloomberg_Field,
       --Origination_Date,
	   Rating_Object,
	   --ISIN_Changed_Ind,
	   --Bond_Number_Old,
	   --Bond_Number,
	   --Lots_Old,
	   --Lots,
	   --Portfolio_Name_Old,
	   --Portfolio_Name,
	   CASE WHEN ISIN_Changed_Ind = 'Y'
	        THEN Bond_Number_Old
			ELSE Bond_Number
			END
	   AS Bond_Number,
	   CASE WHEN ISIN_Changed_Ind = 'Y'
	        THEN Lots_Old
			ELSE Lots
			END
	   AS Lots,
       CASE WHEN ISIN_Changed_Ind = 'Y'
	        THEN Portfolio_Name_Old
			ELSE Portfolio_Name
		   	END
	   AS Portfolio_Name,
	   Rating,
       Rating_Date,
       Rating_Org
from   Bond_Rating_Info A57,temp2
where  A57.Rating_Type = '{Rating_Type.A.GetDescription()}'
and    A57.Version = (select MAX(Version) from Bond_Rating_Info where Report_Date = temp2.Report_Date)
and    A57.Report_Date = temp2.Report_Date
and    A57.Grade_Adjust is not null
--group by
--       RTG_Bloomberg_Field,
--       Origination_Date,
--	     Rating_Object,
--	     ISIN_Changed_Ind,
--	     Bond_Number_Old,
--	     Bond_Number,
--	     Lots_Old,
--	     Lots,
--	     Portfolio_Name_Old,
--	     Portfolio_Name,
--	     Rating,
--       Rating_Date,
--       Rating_Org
),
T1 AS (
   Select BA_Info.Reference_Nbr AS Reference_Nbr ,
          BA_Info.Bond_Number AS Bond_Number,
		  BA_Info.Lots AS Lots,
		  BA_Info.Portfolio AS Portfolio,
		  BA_Info.Segment_Name AS Segment_Name,
		  BA_Info.Bond_Type AS Bond_Type,
		  BA_Info.Lien_position AS Lien_position,
		  BA_Info.Origination_Date AS Origination_Date,
          BA_Info.Report_Date AS Report_Date,

		  --RA_Info.Rating_Date AS Rating_Date,
          --CASE WHEN RA_Info.Rating_Object is null
          --     THEN ' '
          --     ELSE RA_Info.Rating_Object
          --END AS Rating_Object,
          --RA_Info.Rating_Org AS Rating_Org,
		  --oldA57.Rating AS Rating,
		  --(CASE WHEN RA_Info.Rating_Org in ('{RatingOrg.SP.GetDescription()}','{RatingOrg.Moody.GetDescription()}','{RatingOrg.Fitch.GetDescription()}') THEN '國外'
	      --      WHEN RA_Info.Rating_Org in ('{RatingOrg.FitchTwn.GetDescription()}','{RatingOrg.CW.GetDescription()}') THEN '國內'
          --      ELSE ' '
	      --END) AS Rating_Org_Area,
          --CASE WHEN RA_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
          --     THEN GMooInfo2.PD_Grade
          --ELSE GMapInfo.PD_Grade 
          --END AS PD_Grade,
          --CASE WHEN RA_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
          --     THEN GMooInfo2.Grade_Adjust
          --ELSE GMooInfo.Grade_Adjust
          --END AS Grade_Adjust,

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
          CASE WHEN oldA57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
               THEN GMooInfo2.PD_Grade
          ELSE GMapInfo.PD_Grade 
          END AS PD_Grade,
          CASE WHEN oldA57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
               THEN GMooInfo2.Grade_Adjust
          ELSE GMooInfo.Grade_Adjust
          END AS Grade_Adjust,

		  CASE WHEN RISI.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else RISI.ISSUER_TICKER END AS ISSUER_TICKER,
		  CASE WHEN RISI.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else RISI.GUARANTOR_NAME END AS GUARANTOR_NAME,
		  CASE WHEN RISI.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else RISI.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
		  BA_Info.Portfolio_Name AS Portfolio_Name,

          --CASE WHEN RA_Info.RTG_Bloomberg_Field is null
          --     THEN ' '
          --     ELSE RA_Info.RTG_Bloomberg_Field
          --END AS RTG_Bloomberg_Field,

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
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old
   from  Bond_Account_Info BA_Info --A41

   --Left Join Rating_Info RA_Info --A53
   --on BA_Info.Bond_Number = RA_Info.Bond_Number
   --AND BA_Info.Report_Date = RA_Info.Report_Date
   --AND RA_Info.Rating_Date <= BA_Info.Origination_Date --2017/11/15 要符合 評等資料時點 小於等於 債券購入(認列)日期
   --Left Join temp oldA57 --oldA57
   --on RA_Info.RTG_Bloomberg_Field = oldA57.RTG_Bloomberg_Field
   --AND BA_Info.Origination_Date = oldA57.Origination_Date
   --AND RA_Info.Rating_Object = oldA57.Rating_Object

   Left Join temp oldA57 --oldA57
   ON  BA_Info.Bond_Number =  oldA57.Bond_Number
   AND BA_Info.Lots = oldA57.Lots
   AND BA_Info.Portfolio_Name = oldA57.Portfolio_Name

   Left Join Grade_Mapping_Info GMapInfo --A52

   --on RA_Info.Rating_Org = GMapInfo.Rating_Org

   on oldA57.Rating_Org = GMapInfo.Rating_Org

   AND oldA57.Rating = GMapInfo.Rating
   Left Join Grade_Moody_Info GMooInfo --A51
   on GMapInfo.PD_Grade = GMooInfo.PD_Grade
   and GMooInfo.Effective = 'Y'  --年度 最新年度
   -- and Year(BA_Info.Report_Date) = GMooInfo.Data_Year  --年度 目前作法是最新年度
   Left Join Grade_Moody_Info GMooInfo2 --A51Second
   on GMooInfo2.Rating = oldA57.Rating
   and oldA57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
   and GMooInfo2.Effective = 'Y'  --年度 最新年度
   -- and Year(BA_Info.Report_Date) = GMooInfo2.Data_Year  --年度 目前作法是最新年度
   Left Join Rating_Info_SampleInfo RISI --A53(Sample)
   on BA_Info.Bond_Number = RISI.Bond_Number
   AND BA_Info.Report_Date = RISI.Report_Date 
   -- AND RA_Info.Rating_Object = '{RatingObject.Bonds.GetDescription()}'
   Where BA_Info.Report_Date = '{reportData}'
   And   BA_Info.Version = {ver}
   AND   BA_Info.ISIN_Changed_Ind is null
UNION ALL
      Select BA_Info.Reference_Nbr AS Reference_Nbr ,
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
          CASE WHEN oldA57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
               THEN GMooInfo2.PD_Grade
          ELSE GMapInfo.PD_Grade 
          END AS PD_Grade,
          CASE WHEN oldA57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
               THEN GMooInfo2.Grade_Adjust
          ELSE GMooInfo.Grade_Adjust
          END AS Grade_Adjust,
		  RISI.ISSUER_TICKER  AS ISSUER_TICKER,
		  RISI.GUARANTOR_NAME  AS GUARANTOR_NAME,
		  RISI.GUARANTOR_EQY_TICKER  AS GUARANTOR_EQY_TICKER,
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
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old
   from  Bond_Account_Info BA_Info --A41

   Left Join temp oldA57 --oldA57 
   ON BA_Info.Bond_Number_Old = oldA57.Bond_Number
   AND BA_Info.Lots_Old = oldA57.Lots
   AND BA_Info.Portfolio_Name_Old = oldA57.Portfolio_Name

   Left Join Grade_Mapping_Info GMapInfo --A52
   on oldA57.Rating_Org = GMapInfo.Rating_Org
   AND oldA57.Rating = GMapInfo.Rating
   Left Join Grade_Moody_Info GMooInfo --A51
   on GMapInfo.PD_Grade = GMooInfo.PD_Grade
   and GMooInfo.Effective = 'Y'  --年度 最新年度
   Left Join Grade_Moody_Info GMooInfo2 --A51Second
   on GMooInfo2.Rating = oldA57.Rating
   and GMooInfo2.Effective = 'Y'  --年度 最新年度
   and oldA57.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
   Left Join Rating_Info_SampleInfo RISI --A53(Sample)
   on BA_Info.Bond_Number = RISI.Bond_Number
   AND BA_Info.Report_Date = RISI.Report_Date 

   -- AND RA_Info.Rating_Object = '{RatingObject.Bonds.GetDescription()}'

   Where BA_Info.Report_Date = '{reportData}'
   And   BA_Info.Version = {ver}
   AND   BA_Info.ISIN_Changed_Ind = 'Y'
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
           Portfolio_Name_Old)
Select     Reference_Nbr,
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
           Portfolio_Name_Old
		   From
		   T1;
WITH T0 AS (
   Select BA_Info.Reference_Nbr AS Reference_Nbr ,
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
          CASE WHEN RA_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
               THEN GMooInfo2.PD_Grade
          ELSE GMapInfo.PD_Grade 
          END AS PD_Grade,
          CASE WHEN RA_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
               THEN GMooInfo2.Grade_Adjust
          ELSE GMooInfo.Grade_Adjust
          END AS Grade_Adjust,
		  CASE WHEN RISI.ISSUER_TICKER in ('N.S.', 'N.A.') THEN null Else RISI.ISSUER_TICKER END AS ISSUER_TICKER,
		  CASE WHEN RISI.GUARANTOR_NAME in ('N.S.', 'N.A.') THEN null Else RISI.GUARANTOR_NAME END AS GUARANTOR_NAME,
		  CASE WHEN RISI.GUARANTOR_EQY_TICKER in ('N.S.', 'N.A.') THEN null Else RISI.GUARANTOR_EQY_TICKER END AS GUARANTOR_EQY_TICKER,
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
          BA_Info.Portfolio_Name_Old AS Portfolio_Name_Old
   from  Bond_Account_Info BA_Info --A41
   Left Join Rating_Info RA_Info --A53
   on BA_Info.Bond_Number = RA_Info.Bond_Number
   AND BA_Info.Report_Date = RA_Info.Report_Date
   Left Join Grade_Mapping_Info GMapInfo --A52
   on RA_Info.Rating_Org = GMapInfo.Rating_Org
   AND RA_Info.Rating = GMapInfo.Rating
   Left Join Grade_Moody_Info GMooInfo --A51
   on GMapInfo.PD_Grade = GMooInfo.PD_Grade
   and GMooInfo.Effective = 'Y'  --年度 最新年度
   -- and Year(BA_Info.Report_Date) = GMooInfo.Data_Year  --年度 目前作法是最新年度
   Left Join Grade_Moody_Info GMooInfo2 --A51Second
   on GMooInfo2.Rating = RA_Info.Rating
   and RA_Info.Rating_Org = '{RatingOrg.Moody.GetDescription()}'
   and GMooInfo2.Effective = 'Y'  --年度 最新年度
   -- and Year(BA_Info.Report_Date) = GMooInfo2.Data_Year  --年度 目前作法是最新年度
   Left Join Rating_Info_SampleInfo RISI --A53 Sample
   on BA_Info.Bond_Number = RISI.Bond_Number
   AND BA_Info.Report_Date = RISI.Report_Date 
   -- AND RA_Info.Rating_Object = '{RatingObject.Bonds.GetDescription()}'
   Where BA_Info.Report_Date = '{reportData}'
   And   BA_Info.Version = {ver}
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
           Portfolio_Name_Old)
Select     Reference_Nbr,
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
           Portfolio_Name_Old
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
              Portfolio_Name_Old
			)
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
       '{startTime.ToString("yyyy/MM/dd")}',
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
       BR_Info.Portfolio_Name_Old
From   Bond_Rating_Info BR_Info
LEFT JOIN  Bond_Rating_Parm BR_Parm
on         BR_Info.Parm_ID = BR_Parm.Parm_ID
AND        BR_Info.Rating_Object = BR_Parm.Rating_Object
--AND        BR_Info.Rating_Org_Area = BR_Parm.Rating_Org_Area  --2017/11/01修改為不分國內外
Where  BR_Info.Report_Date = '{reportData}'
AND    BR_Info.Version = {ver}
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
         BR_Info.Portfolio_Name_Old ;

--If issuer='GOV-TW-CEN' or 'GOV-Kaohsiung' or 'GOV-TAIPEI' then他們的債項評等放他們發行人的評等
WITH A58ISSUER AS
(
   select *
   from Bond_Rating_Summary
   where Report_Date = '{reportData}'
   and Version = {ver}
   and ISSUER IN('GOV-TW-CEN', 'GOV-Kaohsiung', 'GOV-TAIPEI')
   and Rating_Object = '{RatingObject.ISSUER.GetDescription()}'
)
update Bond_Rating_Summary
set Bond_Rating_Summary.Grade_Adjust = 
A58ISSUER.Grade_Adjust
from Bond_Rating_Summary, A58ISSUER
where Bond_Rating_Summary.Report_Date = '{reportData}'
and Bond_Rating_Summary.Version = {ver}
and Bond_Rating_Summary.ISSUER IN ('GOV-TW-CEN', 'GOV-Kaohsiung', 'GOV-TAIPEI')
and Bond_Rating_Summary.ISSUER = A58ISSUER.ISSUER
and Bond_Rating_Summary.Bond_Number = A58ISSUER.Bond_Number
and Bond_Rating_Summary.Lots = A58ISSUER.Lots
and Bond_Rating_Summary.Reference_Nbr = A58ISSUER.Reference_Nbr
and Bond_Rating_Summary.Portfolio_Name = A58ISSUER.Portfolio_Name
and Bond_Rating_Summary.Rating_Object = '{RatingObject.Bonds.GetDescription()}'
and Bond_Rating_Summary.Rating_Type = A58ISSUER.Rating_Type
and Bond_Rating_Summary.Rating_Org_Area = A58ISSUER.Rating_Org_Area;
                        ";

                        
                        db.Database.CommandTimeout = 300;
                        db.Database.ExecuteSqlCommand(sql);
                        db.Dispose();
                        log.saveTransferCheck(
                            TableType.A57.ToString(),
                            true,
                            dt,
                            version,
                            startTime,
                            DateTime.Now);
                        log.saveTransferCheck(
                            TableType.A58.ToString(),
                            true,
                            dt,
                            version,
                            startTime,
                            DateTime.Now);
                        log.txtLog(
                           TableType.A57.ToString(),
                           true,
                           startTime,
                           A57logPath,
                           MessageType.Success.GetDescription());
                        log.txtLog(
                           TableType.A58.ToString(),
                           true,
                           startTime,
                           A58logPath,
                           MessageType.Success.GetDescription());
                    }
                    catch (Exception ex)
                    {
                        log.saveTransferCheck(
                            TableType.A57.ToString(),
                            false,
                            dt,
                            version,
                            startTime,
                            DateTime.Now);
                        log.saveTransferCheck(
                            TableType.A58.ToString(),
                            false,
                            dt,
                            version,
                            startTime,
                            DateTime.Now);
                        log.txtLog(
                            TableType.A57.ToString(),
                            false,
                            startTime,
                            A57logPath,
                            $"message: {ex.Message}" +
                            $", inner message {ex.InnerException?.InnerException?.Message}");
                        log.txtLog(
                            TableType.A58.ToString(),
                            false,
                            startTime,
                            A58logPath,
                            $"message: {ex.Message}" +
                            $", inner message {ex.InnerException?.InnerException?.Message}");
                    }
                }
                else
                {
                    log.saveTransferCheck(
                        TableType.A57.ToString(),
                        false,
                        dt,
                        version,
                        startTime,
                        DateTime.Now);
                    log.saveTransferCheck(
                        TableType.A58.ToString(),
                        false,
                        dt,
                        version,
                        startTime,
                        DateTime.Now);
                    log.txtLog(
                        TableType.A57.ToString(),
                        false,
                        startTime,
                        A57logPath,
                        MessageType.not_Find_Any.GetDescription("A41"));
                    log.txtLog(
                        TableType.A58.ToString(),
                        false,
                        startTime,
                        A58logPath,
                        MessageType.not_Find_Any.GetDescription("A41"));
                }
            }
            else
            {
                log.saveTransferCheck(
                    TableType.A57.ToString(),
                    false,
                    dt,
                    version,
                    startTime,
                    DateTime.Now);
                log.saveTransferCheck(
                    TableType.A58.ToString(),
                    false,
                    dt,
                    version,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    TableType.A57.ToString(),
                    false,
                    startTime,
                    A57logPath,
                    MessageType.transferError.GetDescription());
                log.txtLog(
                    TableType.A58.ToString(),
                    false,
                    startTime,
                    A58logPath,
                    MessageType.transferError.GetDescription());
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
                    log.txtLog(
                       TableType.C03Mortgage.ToString(),
                       false,
                       startTime,
                       logPath,
                       "Loan_default_Info 無 " + startYearQuartly + " ~ " + endYearQuartly + " 的資料");
                }
                else if (!A07Data.Any())
                {
                    log.txtLog(
                       TableType.C03Mortgage.ToString(),
                       false,
                       startTime,
                       logPath,
                       "Econ_Domestic 無 " + startYearQuartly + " ~ " + endYearQuartly + " 的資料");
                }
                else
                {
                    string productCode = GroupProductCode.M.GetDescription();
                    Group_Product_Code_Mapping gpcm = db.Group_Product_Code_Mapping.Where(x => x.Group_Product_Code.StartsWith(productCode)).FirstOrDefault();
                    if (gpcm == null)
                    {
                        log.txtLog(
                           TableType.C03Mortgage.ToString(),
                           false,
                           startTime,
                           logPath,
                           "Group_Product_Code_Mapping 無房貸的 Product_Code");
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

                        log.txtLog(
                           TableType.C03Mortgage.ToString(),
                           true,
                           startTime,
                           logPath,
                           MessageType.Success.GetDescription());
                    }
                }
            }
            catch (Exception ex)
            {
                log.txtLog(
                   TableType.C03Mortgage.ToString(),
                   false,
                   startTime,
                   logPath,
                   ex.Message);
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