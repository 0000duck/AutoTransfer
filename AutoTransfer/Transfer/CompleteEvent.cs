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

        /// <summary>
        /// A53 save 後續動作(save A57,A58)
        /// </summary>
        /// <param name="reportDate"></param>
        /// <param name="A53Data"></param>
        /// <param name="sampleInfos"></param>
        public void saveDb(DateTime reportDate, List<Rating_Info> A53Data, List<A53.sampleInfo> sampleInfos)
        {
            List<string> nullarr = new List<string>() { "N.S.", "N.A." };
            List<Bond_Rating_Info> A57Data = new List<Bond_Rating_Info>();
            List<Bond_Rating_Summary> A58Data = new List<Bond_Rating_Summary>();
            List<Bond_Account_Info> A41Data = db.Bond_Account_Info
                .Where(x => x.Report_Date == reportDate).ToList();
            string parmID = getParmID(); //選取離今日最近的D60
            if (A41Data.Any())
            {
                try
                {
                    A41Data.ForEach(x =>
                    {
                        A53Data.Where(y => y.Bond_Number.Equals(x.Bond_Number))
                        .ToList()
                        .ForEach(z =>
                        {
                            #region A57 Bond_Rating_Info

                            #region Search A52(Grade_Mapping_Info)

                            var A52 = db.Grade_Mapping_Info
                                        .Where(i => i.Rating_Org.Equals(z.Rating_Org) &&
                                                    i.Rating.Equals(i.Rating))
                                                     .FirstOrDefault();
                            int? PD_Grade = null;
                            if (A52 != null)
                                PD_Grade = A52.PD_Grade;

                            #endregion Search A52(Grade_Mapping_Info)

                            #region Search A51(Grade_Moody_Info)

                            int? Grade_Adjust = null;
                            var Grade_Moody_Info = db.Grade_Moody_Info
                                                   .Where(j => PD_Grade == j.PD_Grade)
                                                   .FirstOrDefault();
                            if (Grade_Moody_Info != null)
                                Grade_Adjust = Grade_Moody_Info.Grade_Adjust;

                            #endregion Search A51(Grade_Moody_Info)

                            string rating_Org_Area = formatOrgArea(z.Rating_Org);
                            sampleInfo info = formateSampleInfo(sampleInfos, z, nullarr);

                            A57Data.Add(
                                new Bond_Rating_Info()
                                {
                                    Reference_Nbr = x.Reference_Nbr,
                                    Bond_Number = x.Bond_Number,
                                    Lots = x.Lots,
                                    Portfolio = x.Portfolio,
                                    Segment_Name = x.Segment_Name,
                                    Bond_Type = x.Bond_Type,
                                    Lien_position = x.Lien_position,
                                    Origination_Date = x.Origination_Date,
                                    Report_Date = reportDate,
                                    Rating_Date = z.Rating_Date,
                                    Rating_Type = "1",
                                    Rating_Object = z.Rating_Object,
                                    Rating_Org = z.Rating_Org,
                                    Rating = z.Rating,
                                    Rating_Org_Area = rating_Org_Area,
                                    //Fill_up_YN = 是否人工補登 (應用系統補登)
                                    //Fill_up_Date = 人工補登日期 (應用系統補登)
                                    PD_Grade = PD_Grade,
                                    Grade_Adjust = Grade_Adjust,
                                    ISSUER_TICKER = info.ISSUER_TICKER,
                                    GUARANTOR_NAME = info.GUARANTOR_NAME,
                                    GUARANTOR_EQY_TICKER = info.GUARANTOR_EQY_TICKER,
                                    Parm_ID = parmID,
                                    Portfolio_Name = "",
                                    RTG_Bloomberg_Field = z.RTG_Bloomberg_Field,
                                    SMF = "",
                                    ISSUER = "",
                                    Version = x.Version,
                                    Security_Ticker = ""
                                });

                            //查詢到舊的Bond_Rating_Info
                            var oldA57 = db.Bond_Rating_Info
                                           .Where(m => m.Rating_Date == z.Rating_Date)
                                           .FirstOrDefault();

                            A57Data.Add(
                                new Bond_Rating_Info()
                                {
                                    Reference_Nbr = x.Reference_Nbr,
                                    Bond_Number = x.Bond_Number,
                                    Lots = x.Lots,
                                    Portfolio = x.Portfolio,
                                    Segment_Name = x.Segment_Name,
                                    Bond_Type = x.Bond_Type,
                                    Lien_position = x.Lien_position,
                                    Origination_Date = x.Origination_Date,
                                    Report_Date = reportDate,
                                    Rating_Type = "2",
                                    Rating_Object = z.Rating_Object,
                                    Rating_Org = z.Rating_Org,
                                    Rating = oldA57 != null ? oldA57.Rating : null,
                                    Rating_Org_Area = rating_Org_Area,
                                    //Fill_up_YN = 是否人工補登 (應用系統補登)
                                    //Fill_up_Date = 人工補登日期 (應用系統補登)
                                    PD_Grade = PD_Grade,
                                    Grade_Adjust = Grade_Adjust,
                                    ISSUER_TICKER = info.ISSUER_TICKER,
                                    GUARANTOR_NAME = info.GUARANTOR_NAME,
                                    GUARANTOR_EQY_TICKER = info.GUARANTOR_EQY_TICKER,
                                    Parm_ID = parmID,
                                    Portfolio_Name = "",
                                    RTG_Bloomberg_Field = z.RTG_Bloomberg_Field,
                                    SMF = "",
                                    ISSUER = "",
                                    Version = x.Version,
                                    Security_Ticker = ""
                                });

                            #endregion A57 Bond_Rating_Info
                        });
                    });

                    A41Data.ForEach(x =>
                    {
                        A53Data.Where(y => y.Bond_Number.Equals(x.Bond_Number))
                               .ToList()
                               .ForEach(z =>
                               {
                                   #region A58 Bond_Rating_Summary

                                   string rating_Org_Area = formatOrgArea(z.Rating_Org);

                                   #region Search D60-信評優先選擇參數檔

                                   #region Rating_Selection && Rating_Priority

                                   string ratingSelection = string.Empty; //1:孰高 2:孰低
                                   int ratingPriority = 0; //優先順序
                                   Bond_Rating_Parm parm = db.Bond_Rating_Parm.Where(i =>
                                                             i.Parm_ID == parmID &&
                                                             i.Rating_Object == z.Rating_Object &&
                                                             i.Rating_Org_Area == rating_Org_Area).FirstOrDefault();
                                   if (parm != null)
                                   {
                                       ratingSelection = parm.Rating_Selection;
                                       ratingPriority = parm.Rating_Priority ?? 0;
                                   }

                                   #endregion Rating_Selection && Rating_Priority

                                   #endregion Search D60-信評優先選擇參數檔

                                   #region Search A57

                                   List<Bond_Rating_Info> A57Datas = new List<Bond_Rating_Info>();
                                   A57Datas = A57Data.Where(y => y.Bond_Number == x.Bond_Number &&
                                                                 y.Rating_Object == z.Rating_Object).ToList();
                                   int? gradeAdjust = null;
                                   //如果 Rating_Selection = '1' 代表取孰高評等
                                   //SELECT MIN(Grade_Adjust) GROUP BY  Rating_Object
                                   //Where Grade_Adjust 分別為 發行人,債項,保證人;
                                   if (ratingSelection.Equals("1"))
                                   {
                                       gradeAdjust = A57Datas.Min(i => i.Grade_Adjust);
                                   }
                                   //如果 Rating_Selection = '2' 代表取孰低評等
                                   //SELECT Max(Grade_Adjust) GROUP BY  Rating_Object
                                   //Where Grade_Adjust 分別為 發行人,債項,保證人;
                                   if (ratingSelection.Equals("2"))
                                   {
                                       gradeAdjust = A57Datas.Max(i => i.Grade_Adjust);
                                   }

                                   #endregion Search A57

                                   A58Data.Add(
                                       new Bond_Rating_Summary()
                                       {
                                           Reference_Nbr = x.Reference_Nbr,
                                           Report_Date = reportDate,
                                           Parm_ID = parmID,
                                           Bond_Type = x.Bond_Type,
                                           Rating_Type = "1",
                                           Rating_Object = z.Rating_Object,
                                           Rating_Org_Area = rating_Org_Area,
                                           Rating_Selection = ratingSelection,
                                           Grade_Adjust = gradeAdjust,
                                           Rating_Priority = ratingPriority,
                                           //Processing_Date = 轉檔時寫入
                                           Version = x.Version,
                                           Bond_Number = x.Bond_Number,
                                           Lots = x.Lots,
                                           Portfolio = x.Portfolio,
                                           Origination_Date = x.Origination_Date,
                                           Portfolio_Name = "",
                                           SMF = "",
                                           ISSUER = ""
                                       });

                                   A58Data.Add(
                                         new Bond_Rating_Summary()
                                         {
                                             Reference_Nbr = x.Reference_Nbr,
                                             Report_Date = reportDate,
                                             Parm_ID = parmID,
                                             Bond_Type = x.Bond_Type,
                                             Rating_Type = "1",
                                             Rating_Object = z.Rating_Object,
                                             Rating_Org_Area = rating_Org_Area,
                                             Rating_Selection = ratingSelection,
                                             Grade_Adjust = gradeAdjust,
                                             Rating_Priority = ratingPriority,
                                             //Processing_Date = 轉檔時寫入
                                             Version = x.Version,
                                             Bond_Number = x.Bond_Number,
                                             Lots = x.Lots,
                                             Portfolio = x.Portfolio,
                                             Origination_Date = x.Origination_Date,
                                             Portfolio_Name = "",
                                             SMF = "",
                                             ISSUER = ""
                                         });

                                   #endregion A58 Bond_Rating_Summary
                               });
                    });
                    db.Bond_Rating_Info.AddRange(A57Data);
                    db.Bond_Rating_Summary.AddRange(A58Data);
                    db.SaveChanges();
                    db.Dispose();
                }
                catch
                {
                }
            }
        }

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
            if (RatingObject.Bonds.GetDescription().Equals(info.Rating_Org))
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
    }
}