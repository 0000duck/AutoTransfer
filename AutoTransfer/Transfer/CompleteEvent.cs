using System;
using System.Collections.Generic;
using System.Linq;
using AutoTransfer.Utility;
using static AutoTransfer.Enum.Ref;

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
        public void saveDb(DateTime reportDate, List<Rating_Info> A53Data)
        {
            List<Bond_Rating_Info> A57Data = new List<Bond_Rating_Info>();
            List<Bond_Rating_Summary> A58Data = new List<Bond_Rating_Summary>();
            List<Bond_Account_Info> A41Data = db.Bond_Account_Info
                .Where(x => x.Report_Date == reportDate).ToList();
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
                            int PD_Grade = db.Grade_Mapping_Info
                                             .Where(i => i.Rating_Org.Equals(z.Rating_Org)
                                              && i.Rating.Equals(i.Rating)).First().PD_Grade;
                            string rating_Org_Area = formatOrgArea(z.Rating_Org);
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
                                    //Rating_Type = ETL給定
                                    Rating_Object = z.Rating_Object,
                                    Rating_Org = z.Rating_Org,
                                    Rating = z.Rating,
                                    Rating_Org_Area = rating_Org_Area,
                                    //Fill_up_YN = 是否人工補登 (應用系統補登)
                                    //Fill_up_Date = 人工補登日期 (應用系統補登)
                                    PD_Grade = PD_Grade,
                                    Grade_Adjust = db.Grade_Moody_Info
                                                    .Where(j => PD_Grade == j.PD_Grade)
                                                    .First()
                                                    .Grade_Adjust
                                    //ISSUER_TICKER = 
                                    //GUARANTOR_NAME = 
                                    //GUARANTOR_EQY_TICKER = 
                                });
                            #endregion

                            #region A58 Bond_Rating_Summary

                            string Parm_ID = "Parm001";

                            var parm = db.Bond_Rating_Parm.Where(i =>
                            i.Parm_ID == Parm_ID &&
                            i.Rating_Object == z.Rating_Object &&
                            i.Rating_Org_Area == rating_Org_Area).FirstOrDefault();

                            A58Data.Add(
                                new Bond_Rating_Summary()
                                {
                                    Reference_Nbr = x.Reference_Nbr,
                                    Bond_Number = x.Bond_Number,
                                    Lots = x.Lots,
                                    Portfolio = x.Portfolio,
                                    Origination_Date = x.Origination_Date,
                                    Report_Date = reportDate,
                                    //Parm_ID = //D60-信評優先選擇參數檔
                                    Bond_Type = x.Bond_Type,
                                    //Rating_Type = A57 Rating_Type
                                    Rating_Object = z.Rating_Object,
                                    Rating_Org_Area = rating_Org_Area,
                                    Rating_Selection = parm.Rating_Selection,
                                    //Grade_Adjust
                                    Rating_Priority = parm.Rating_Priority,
                                    //Processing_Date = 轉檔時寫入
                                });
                            #endregion
                        });
                    });
                    db.Bond_Rating_Info.AddRange(A57Data);
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
    }
}
