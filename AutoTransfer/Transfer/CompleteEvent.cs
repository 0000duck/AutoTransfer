using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
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
        /// <param name="reportDate"></param>
        /// <param name="version"></param>
        /// <param name="A53Data"></param>
        /// <param name="sampleInfos"></param>
        public void saveDb(DateTime reportDate, int version, List<Rating_Info> A53Data, List<A53.sampleInfo> sampleInfos)
        {
            A57logPath = log.txtLocation(TableType.A57.ToString());
            A58logPath = log.txtLocation(TableType.A58.ToString());

            List<string> nullarr = new List<string>() { "N.S.", "N.A." };
            List<Bond_Rating_Info> A57Data = new List<Bond_Rating_Info>();
            List<Bond_Rating_Summary> A58Data = new List<Bond_Rating_Summary>();
            DateTime startTime = DateTime.Now;

            string parmID = getParmID(); //選取離今日最近的D60
            if (log.checkTransferCheck(TableType.A57.ToString(), TableType.A53.ToString(), reportDate, version) &&
                log.checkTransferCheck(TableType.A58.ToString(), TableType.A53.ToString(), reportDate, version))
            {
                List<Bond_Account_Info> A41Data = db.Bond_Account_Info
                    .Where(x => x.Report_Date == reportDate &&
                                 x.Version != null &&
                                 x.Version == version).ToList();
                if (A41Data.Any())
                {
                    try
                    {
                        var A52Data = db.Grade_Mapping_Info.ToList();
                        var A51Data = db.Grade_Moody_Info.ToList();
                        var D60Data = db.Bond_Rating_Parm.ToList();
                        A41Data.ForEach(x =>
                        {
                            A53Data.Where(y => y.Bond_Number.Equals(x.Bond_Number))
                            .ToList()
                            .ForEach(z =>
                            {
                                #region A57 Bond_Rating_Info

                                #region Search A52(Grade_Mapping_Info)

                                var A52 = A52Data.Where(i => i.Rating_Org.Equals(z.Rating_Org) &&
                                                             i.Rating.Equals(z.Rating))
                                                             .FirstOrDefault();
                                int? PD_Grade = null;
                                if (A52 != null)
                                    PD_Grade = A52.PD_Grade;

                                #endregion Search A52(Grade_Mapping_Info)

                                #region Search A51(Grade_Moody_Info)

                                int? Grade_Adjust = null;
                                var Grade_Moody_Info = A51Data
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
                                        Portfolio_Name = x.Portfolio_Name,
                                        RTG_Bloomberg_Field = z.RTG_Bloomberg_Field,
                                        SMF = x.PRODUCT,
                                        ISSUER = x.ISSUER,
                                        Version = x.Version,
                                        Security_Ticker = getSecurityTicker(x.PRODUCT, x.Bond_Number)
                                    });

                                //查詢到舊的Bond_Rating_Info
                                var oldA57 = db.Bond_Rating_Info
                                                   .Where(m => m.Bond_Number == z.Bond_Number &&
                                                               m.Lots == x.Lots &&
                                                               m.Portfolio_Name == x.Portfolio_Name &&
                                                               x.Origination_Date != null &&
                                                               m.Origination_Date == x.Origination_Date &&
                                                               m.Rating_Type == "2"
                                                   )
                                                   .FirstOrDefault();

                                #region Search A52(Grade_Mapping_Info)
                                if (oldA57 != null)
                                    A52 = A52Data.Where(i => i.Rating_Org.Equals(z.Rating_Org) &&
                                                             i.Rating.Equals(oldA57.Rating))
                                                              .FirstOrDefault();
                                else
                                    A52 = null;
                                PD_Grade = null;
                                if (A52 != null)
                                    PD_Grade = A52.PD_Grade;

                                #endregion Search A52(Grade_Mapping_Info)

                                #region Search A51(Grade_Moody_Info)

                                Grade_Adjust = null;
                                Grade_Moody_Info =  A51Data.Where(j => PD_Grade == j.PD_Grade)
                                                           .FirstOrDefault();
                                if (Grade_Moody_Info != null)
                                    Grade_Adjust = Grade_Moody_Info.Grade_Adjust;

                                #endregion Search A51(Grade_Moody_Info)

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
                                        Portfolio_Name = x.Portfolio_Name,
                                        RTG_Bloomberg_Field = z.RTG_Bloomberg_Field,
                                        SMF = x.PRODUCT,
                                        ISSUER = x.ISSUER,
                                        Version = x.Version,
                                        Security_Ticker = getSecurityTicker(x.PRODUCT, x.Bond_Number)
                                    });

                                #endregion A57 Bond_Rating_Info
                            });
                        });

                        foreach (var item in A57Data
                            .GroupBy(x => new
                            {
                                x.Reference_Nbr,
                                x.Report_Date,
                                x.Parm_ID,
                                x.Bond_Type,
                                x.Rating_Type,
                                x.Rating_Object,
                                x.Rating_Org_Area,
                                x.Version,
                                x.Bond_Number,
                                x.Lots,
                                x.Portfolio,
                                x.Origination_Date,
                                x.Portfolio_Name,
                                x.SMF,
                                x.ISSUER
                            }))
                        {
                            #region Rating_Selection && Rating_Priority

                            string ratingSelection = string.Empty; //1:孰高 2:孰低
                            int ratingPriority = 0; //優先順序
                                                    //D60(信評優先選擇參數檔)
                            Bond_Rating_Parm parm = D60Data
                            .Where(i => i.Parm_ID == parmID &&
                                        i.Rating_Object == item.Key.Rating_Object &&
                                        i.Rating_Org_Area == item.Key.Rating_Org_Area).FirstOrDefault();
                            if (parm != null)
                            {
                                ratingSelection = parm.Rating_Selection;
                                ratingPriority = parm.Rating_Priority ?? 0;
                            }

                            #endregion Rating_Selection && Rating_Priority

                            int? gradeAdjust = null;

                            //如果 Rating_Selection = '1' 代表取孰高評等
                            //SELECT MIN(Grade_Adjust) GROUP BY  Rating_Object
                            //Where Grade_Adjust 分別為 發行人,債項,保證人;
                            if (ratingSelection.Equals("1"))
                            {
                                gradeAdjust = item.Min(i => i.Grade_Adjust == null ? 0 : i.Grade_Adjust);
                            }
                            //如果 Rating_Selection = '2' 代表取孰低評等
                            //SELECT Max(Grade_Adjust) GROUP BY  Rating_Object
                            //Where Grade_Adjust 分別為 發行人,債項,保證人;
                            if (ratingSelection.Equals("2"))
                            {
                                gradeAdjust = item.Max(i => i.Grade_Adjust == null ? 0 : i.Grade_Adjust);
                            }
                            if (gradeAdjust == 0)
                                gradeAdjust = null;

                            A58Data.Add(
                            new Bond_Rating_Summary()
                            {
                                Reference_Nbr = item.Key.Reference_Nbr,
                                Report_Date = reportDate,
                                Parm_ID = parmID,
                                Bond_Type = item.Key.Bond_Type,
                                Rating_Type = item.Key.Rating_Type,
                                Rating_Object = item.Key.Rating_Object,
                                Rating_Org_Area = item.Key.Rating_Org_Area,
                                Rating_Selection = ratingSelection,
                                Grade_Adjust = gradeAdjust,
                                Rating_Priority = ratingPriority,
                                //Processing_Date = 轉檔時寫入
                                Version = item.Key.Version,
                                Bond_Number = item.Key.Bond_Number,
                                Lots = item.Key.Lots,
                                Portfolio = item.Key.Portfolio,
                                Origination_Date = item.Key.Origination_Date,
                                Portfolio_Name = item.Key.Portfolio_Name,
                                SMF = item.Key.SMF,
                                ISSUER = item.Key.ISSUER
                            });
                        }

                        db.Bond_Rating_Info.AddRange(A57Data);
                        db.Bond_Rating_Summary.AddRange(A58Data);
                        db.SaveChanges();
                        db.Dispose();
                        log.saveTransferCheck(
                            TableType.A57.ToString(),
                            true,
                            reportDate,
                            version,
                            startTime,
                            DateTime.Now);
                        log.saveTransferCheck(
                            TableType.A58.ToString(),
                            true,
                            reportDate,
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
                    catch (DbUpdateException ex)
                    {
                        log.saveTransferCheck(
                            TableType.A57.ToString(),
                            false,
                            reportDate,
                            version,
                            startTime,
                            DateTime.Now);
                        log.saveTransferCheck(
                            TableType.A58.ToString(),
                            false,
                            reportDate,
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
                        reportDate,
                        version,
                        startTime,
                        DateTime.Now);
                    log.saveTransferCheck(
                        TableType.A58.ToString(),
                        false,
                        reportDate,
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
                    reportDate,
                    version,
                    startTime,
                    DateTime.Now);
                log.saveTransferCheck(
                    TableType.A58.ToString(),
                    false,
                    reportDate,
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

        /// <summary>
        /// C03 Mortgage save
        /// </summary>
        /// <param name="yearQuartly"></param>
        public void saveC03Mortgage(string yearQuartly)
        {
            string logPath = log.txtLocation(TableType.C03Mortgage.ToString());
            DateTime startTime = DateTime.Now;

            try
            {
                List<Loan_default_Info> A06Data = db.Loan_default_Info
                                                  .Where(x => x.Year_Quartly == yearQuartly).ToList();

                List<Econ_Domestic> A07Data = db.Econ_Domestic
                                                  .Where(x => x.Year_Quartly == yearQuartly).ToList();
                if (!A06Data.Any())
                {
                    log.txtLog(
                       TableType.C03Mortgage.ToString(),
                       false,
                       startTime,
                       logPath,
                       "Loan_default_Info 無 " + yearQuartly + " 的資料");
                }
                else if (!A07Data.Any())
                {
                    log.txtLog(
                       TableType.C03Mortgage.ToString(),
                       false,
                       startTime,
                       logPath,
                       "Econ_Domestic 無 " + yearQuartly + " 的資料");
                }
                else
                {
                    string productCode = GroupProductCode.M.GetDescription();
                    productCode = db.Group_Product_Code_Mapping.Where(x => x.Group_Product_Code.StartsWith(productCode)).FirstOrDefault().Product_Code;

                    var query = db.Econ_D_YYYYMMDD
                                .Where(x => x.Year_Quartly == yearQuartly);
                    db.Econ_D_YYYYMMDD.RemoveRange(query);

                    db.Econ_D_YYYYMMDD.AddRange(
                        A07Data.Select(x => new Econ_D_YYYYMMDD()
                        {
                            Processing_Date = DateTime.Now.ToString("yyyy/MM/dd"),
                            Product_Code = productCode,
                            Data_ID = "",
                            Year_Quartly = x.Year_Quartly,
                            PD_Quartly = x.Loan_default_Info.PD_Quartly,
                            var1 = Extension.doubleNToDouble(x.TWSE_Index),
                            var2 = Extension.doubleNToDouble(x.TWRGSARP_Index),
                            var3 = Extension.doubleNToDouble(x.TWGDPCON_Index),
                            var4 = Extension.doubleNToDouble(x.TWLFADJ_Index),
                            var5 = Extension.doubleNToDouble(x.TWCPI_Index),
                            var6 = Extension.doubleNToDouble(x.TWMSA1A_Index),
                            var7 = Extension.doubleNToDouble(x.TWMSA1B_Index),
                            var8 = Extension.doubleNToDouble(x.TWMSAM2_Index),
                            var9 = Extension.doubleNToDouble(x.GVTW10YR_Index),
                            var10 = Extension.doubleNToDouble(x.TWTRBAL_Index),
                            var11 = Extension.doubleNToDouble(x.TWTREXP_Index),
                            var12 = Extension.doubleNToDouble(x.TWTRIMP_Index),
                            var13 = Extension.doubleNToDouble(x.TAREDSCD_Index),
                            var14 = Extension.doubleNToDouble(x.TWCILI_Index),
                            var15 = Extension.doubleNToDouble(x.TWBOPCUR_Index),
                            var16 = Extension.doubleNToDouble(x.EHCATW_Index),
                            var17 = Extension.doubleNToDouble(x.TWINDPI_Index),
                            var18 = Extension.doubleNToDouble(x.TWWPI_Index),
                            var19 = Extension.doubleNToDouble(x.TARSYOY_Index),
                            var20 = Extension.doubleNToDouble(x.TWEOTTL_Index),
                            var21 = Extension.doubleNToDouble(x.SLDETIGT_Index),
                            var22 = Extension.doubleNToDouble(x.TWIRFE_Index),
                            var23 = Extension.doubleNToDouble(x.SINYI_HOUSE_PRICE_index),
                            var24 = Extension.doubleNToDouble(x.CATHAY_ESTATE_index),
                            var25 = Extension.doubleNToDouble(x.Real_GDP2011),
                            var26 = Extension.doubleNToDouble(x.MCCCTW_Index),
                            var27 = Extension.doubleNToDouble(x.TRDR1T_Index)
                        })
                    );

                    db.SaveChanges();

                    log.txtLog(
                       TableType.C03Mortgage.ToString(),
                       true,
                       startTime,
                       logPath,
                       MessageType.Success.GetDescription());
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