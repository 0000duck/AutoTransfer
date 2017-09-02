using AutoTransfer.Abstract;
using AutoTransfer.Commpany;
using AutoTransfer.CreateFile;
using AutoTransfer.Sample;
using AutoTransfer.SFTPConnect;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Threading;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class A53 : SFTPTransfer
    {
        #region 共用參數

        private FormatRating fr = new FormatRating();

        private Dictionary<string, commpayInfo> info =
            new Dictionary<string, commpayInfo>();

        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private int verInt = 0;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A53;
        private string type = TableType.A53.ToString();

        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public override void startTransfer(string dateTime)
        {
            IFRS9Entities db = new IFRS9Entities();
            startTime = DateTime.Now;

            if (dateTime.Length != 8 ||
               !DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
               System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt))
            {
                db.Dispose();
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.DateTime_Format_Fail.GetDescription()
                    );
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1, //A53無版本 (default為1)
                    startTime,
                    DateTime.Now);
            }
            
            var A41 = db.Bond_Account_Info
               .Any(x => x.Report_Date == reportDateDt);
            verInt = db.Bond_Account_Info
                .Where(x => x.Report_Date == reportDateDt && x.Version != null)
                .DefaultIfEmpty().Max(x => x.Version == null ? 0 : x.Version.Value);
            var check = log.checkTransferCheck(TableType.A53.ToString(), "A41", reportDateDt, verInt);
            logPath = log.txtLocation(type);
            if (!A41 ||
               !check || verInt == 0)
            {
                db.Dispose();
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1, //A53只有一版 (default為1)
                    startTime,
                    DateTime.Now);
                List<string> errs = new List<string>();
                if (!A41)
                    errs.Add(MessageType.not_Find_Any.GetDescription());
                if (!check || verInt == 0)
                    errs.Add(MessageType.transferError.GetDescription());
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    string.Join(",", errs)
                    );
            }
            else
            {

                db.Dispose();
                reportDateStr = dateTime;
                setFile = new SetFile(tableType, dateTime);
                createSampleFile();
            }
        }

        /// <summary>
        /// 建立 Sample 要Put的檔案
        /// </summary>
        protected override void createSampleFile()
        {
            //建立  檔案
            // create ex:sampleA53_20170807
            if (new CreateSampleFile().create(tableType, reportDateStr))
            {
                //把資料送給SFTP
                putSampleSFTP();
            }
            else
            {
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Create_Sample_File_Fail.GetDescription());
            }
        }

        /// <summary>
        /// SFTP Put Sample檔案
        /// </summary>
        protected override void putSampleSFTP()
        {
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putSampleFilePath(),
                 setFile.putSampleFileName(),
                 out error);
            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Put_Sample_File_Fail.GetDescription());
            }
            else //success (wait 20 min and get data)
            {
                //t.Interval = 3 * 1000;
                //Action f = () => getSampleSFTP();
                //t.Start(f); //委派 設定時間後要做的動作
                Thread.Sleep(20 * 60 * 1000);
                getSampleSFTP();
            }
        }

        /// <summary>
        /// SFTP Get Sample檔案
        /// </summary>
        protected override void getSampleSFTP()
        {
            new FileRelated().createFile(setFile.getSampleFilePath());
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Get(string.Empty,
                setFile.getSampleFilePath(),
                setFile.getSampleFileName(),
                out error);
            if (!error.IsNullOrWhiteSpace())
            {
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Get_Sample_File_Fail.GetDescription());
            }
            else
            {
                createCommpanyFile();
            }
        }

        /// <summary>
        /// 建立 Commpany 要Put檔案
        /// </summary>
        protected override void createCommpanyFile()
        {
            List<string> data = new List<string>();
            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getSampleFilePath(), setFile.getSampleFileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;
                info = new Dictionary<string, commpayInfo>();
                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                        flag = false;
                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        if (arr.Length >= 18)
                        {
                            if (!arr[3].IsNullOrWhiteSpace() &&
                                !nullarr.Contains(arr[3].Trim()))  //ISSUER_EQUITY_TICKER (發行人)
                            {
                                commpayInfo x = new commpayInfo();
                                if (!info.TryGetValue(arr[3].Trim(), out x))
                                {
                                    data.Add(arr[3].Trim());
                                    info.Add(arr[3].Trim(), new commpayInfo()
                                    {
                                        Bond_Number = new List<string>() { arr[0].Trim() },
                                        Rating_Object = RatingObject.ISSUER.GetDescription()
                                    });
                                }
                                else
                                {
                                    x.Bond_Number.Add(arr[0].Trim());
                                }
                            }
                            if (!arr[6].Trim().IsNullOrWhiteSpace() &&
                                !nullarr.Contains(arr[6].Trim()))  //GUARANTOR_EQY_TICKER (擔保人)
                            {
                                commpayInfo x = new commpayInfo();
                                if (!info.TryGetValue(arr[6].Trim(), out x))
                                {
                                    data.Add(arr[6].Trim());
                                    info.Add(arr[6].Trim(), new commpayInfo()
                                    {
                                        Bond_Number = new List<string>() { arr[0].Trim() },
                                        Rating_Object = RatingObject.GUARANTOR.GetDescription()
                                    });
                                }
                                else
                                {
                                    x.Bond_Number.Add(arr[0].Trim());
                                }
                            }
                        }
                    }
                    if ("START-OF-DATA".Equals(line))
                        flag = true;
                }
            }
            if (new CreateCommpanyFile().create(tableType, reportDateStr, data))
            {
                putCommpanySFTP();
            }
            else
            {
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Create_Commpany_File_Fail.GetDescription());
            }
        }

        /// <summary>
        /// SFTP Put Commpany檔案
        /// </summary>
        protected override void putCommpanySFTP()
        {
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                setFile.putCommpanyFilePath(),
                setFile.putCommpanyFileName(), out error);
            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Put_Commpany_File_Fail.GetDescription());
            }
            else //success (wait 20 min and get data)
            {
                //t.Stop();
                //t.Interval = 5 * 1000;
                //Action f = () => getCommpanySFTP();
                //t.Start(f); //委派 設定時間後要做的動作
                Thread.Sleep(20 * 60 * 1000);
                getCommpanySFTP();
            }
        }

        /// <summary>
        /// SFTP Get Commpany檔案
        /// </summary>
        protected override void getCommpanySFTP()
        {
            new FileRelated().createFile(setFile.getCommpanyFilePath());
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getCommpanyFilePath(),
                 setFile.getCommpanyFileName(),
                 out error);
            if (!error.IsNullOrWhiteSpace())
            {
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Get_Commpanye_File_Fail.GetDescription());
            }
            else
            {
                DataToDb();
            }
        }

        /// <summary>
        /// Db save
        /// </summary>
        protected override void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();
            List<Rating_Info> sampleData = new List<Rating_Info>();
            List<Rating_Info> commpanyData = new List<Rating_Info>();
            List<sampleInfo> sampleInfos = new List<sampleInfo>();
            A53Sample a53Sample = new A53Sample();
            A53Commpany a53Commpany = new A53Commpany();

            #region sample Data

            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getSampleFilePath(), setFile.getSampleFileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                        flag = false;
                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        //arr[0]  ex: US00206RDH21 (債券編號)
                        //arr[1]  ex: 0
                        //arr[2]  ex: 20
                        //arr[3]  ISSUER_EQUITY_TICKER (發行人)
                        //arr[4]  ISSUE_DT (債券評等日期)
                        //arr[5]  ISSUER (債券名稱)
                        //arr[6]  GUARANTOR_EQY_TICKER (擔保人)
                        //arr[7]  GUARANTOR_NAME (擔保人名稱)
                        //--標普(S&P)
                        //arr[8]  (國外)RTG_SP (SP國外評等)
                        //arr[9] (國外)SP_EFF_DT (SP國外評等日期)
                        //--穆迪(Moody's)
                        //arr[10] (國外)RTG_MOODY (Moody's國外評等)
                        //arr[11] (國外)MOODY_EFF_DT (Moody's國外評等日期)
                        //--惠譽台灣(Fitch(twn))
                        //arr[12] (國內)RTG_FITCH_NATIONAL (惠譽國內評等)
                        //arr[13] (國內)RTG_FITCH_NATIONAL_DT (惠譽國內評等日期)
                        //--惠譽(Fitch)
                        //arr[14] (國外)RTG_FITCH (惠譽評等)
                        //arr[15] (國外)FITCH_EFF_DT (惠譽評等日期)
                        //--TRC(中華信評)
                        //arr[16] (國內)RTG_TRC (TRC 評等)
                        //arr[17] (國內)TRC_EFF_DT (TRC 評等日期)
                        if (arr.Length >= 18)
                        {
                            //S&P國外評等
                            validateSample(
                                arr[8],
                                arr[9],
                                arr[0],
                                A53SampleBloombergField.RTG_SP.ToString(),
                                RatingOrg.SP,
                                sampleData);
                            //Moody's國外評等
                            validateSample(
                                arr[10],
                                arr[11],
                                arr[0],
                                A53SampleBloombergField.RTG_MOODY.ToString(),
                                RatingOrg.Moody,
                                sampleData);
                            //惠譽台灣
                            validateSample(
                                arr[12],
                                arr[13],
                                arr[0],
                                A53SampleBloombergField.RTG_FITCH_NATIONAL.ToString(),
                                RatingOrg.FitchTwn,
                                sampleData);
                            //惠譽
                            validateSample(
                                arr[14],
                                arr[15],
                                arr[0],
                                A53SampleBloombergField.RTG_FITCH.ToString(),
                                RatingOrg.Fitch,
                                sampleData);
                            //TRC(中華信評)
                            validateSample(
                                arr[16],
                                arr[17],
                                arr[0],
                                A53SampleBloombergField.RTG_TRC.ToString(),
                                RatingOrg.CW,
                                sampleData);
                            sampleInfos.Add(new sampleInfo()
                            {
                                Bond_Number = arr[0],
                                ISSUER_TICKER = arr[3],
                                GUARANTOR_EQY_TICKER = arr[6],
                                GUARANTOR_NAME = arr[7]
                            });
                        }
                    }
                    if ("START-OF-DATA".Equals(line))
                        flag = true;
                }
            }

            #endregion sample Data

            #region commpany Data

            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getCommpanyFilePath(), setFile.getCommpanyFileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                        flag = false;
                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        //arr[0]  ex: T US Equity (發行人or擔保人)
                        //arr[1]  ex: 0
                        //arr[2]  ex: 38
                        //arr[3]  ID_BB_COMPANY  公司ID
                        //arr[4]  LONG_COMP_NAME  公司名稱
                        //arr[5]  COUNTRY_ISO  城市(國家)
                        //arr[6]  INDUSTRY_GROUP
                        //arr[7]  INDUSTRY_SECTOR
                        //--標普(S&P)
                        //arr[8]  (國外)RTG_SP_LT_LC_ISSUER_CREDIT (標普本國貨幣長期發行人信用評等)
                        //arr[9]  (國外)RTG_SP_LT_LC_ISS_CRED_RTG_DT (標普本國貨幣長期發行人信用評等日期)
                        //arr[10] (國外)RTG_SP_LT_FC_ISSUER_CREDIT (標普長期外幣發行人信用評等)
                        //arr[11] (國外)RTG_SP_LT_FC_ISS_CRED_RTG_DT (標普長期外幣發行人信用評等日期)
                        //--穆迪(Moody's)
                        //arr[12] (國內)RTG_MDY_LOCAL_LT_BANK_DEPOSITS (穆迪長期本國銀行存款評等)
                        //arr[13] (國內)RTG_MDY_LT_LC_BANK_DEP_RTG_DT (穆迪長期本國銀行存款評等日期)
                        //arr[14] (國外)RTG_MDY_FC_CURR_ISSUER_RATING (穆迪外幣發行人評等)
                        //arr[15] (國外)RTG_MDY_FC_CURR_ISSUER_RTG_DT (穆迪外幣發行人評等日期)
                        //arr[16] (國外)RTG_MDY_ISSUER (穆迪發行人評等)
                        //arr[17] (國外)RTG_MDY_ISSUER_RTG_DT (穆迪發行人評等日期)
                        //arr[18] (國外)RTG_MOODY_LONG_TERM (穆迪長期評等)
                        //arr[19] (國外)RTG_MOODY_LONG_TERM_DATE (穆迪長期評等日期)
                        //arr[20] (國外)RTG_MDY_SEN_UNSECURED_DEBT (穆迪優先無擔保債務評等)
                        //arr[21] (國外)RTG_MDY_SEN_UNSEC_RTG_DT (穆迪優先無擔保債務評等日期)
                        //--惠譽(Fitch)
                        //arr[22] (國外)RTG_FITCH_LT_ISSUER_DEFAULT (惠譽長期發行人違約評等)
                        //arr[23] (國外)RTG_FITCH_LT_ISSUER_DFLT_RTG_DT (惠譽長期發行人違約評等日期)
                        //arr[24] (國外)RTG_FITCH_LT_FC_ISSUER_DEFAULT (惠譽長期外幣發行人違約評等)
                        //arr[25] (國外)RTG_FITCH_LT_FC_ISS_DFLT_RTG_DT (惠譽長期外幣發行人違約評等日期)
                        //arr[26] (國外)RTG_FITCH_LT_LC_ISSUER_DEFAULT (惠譽長期本國貨幣發行人違約評等)
                        //arr[27] (國外)RTG_FITCH_LT_LC_ISS_DFLT_RTG_DT (惠譽長期本國貨幣發行人違約評等日期)
                        //arr[28] (國外)RTG_FITCH_SEN_UNSECURED (惠譽優先無擔保債務評等)
                        //arr[29] (國外)RTG_FITCH_SEN_UNSEC_RTG_DT (惠譽優先無擔保債務評等日期)
                        //--惠譽台灣(Fitch(twn))
                        //arr[30] (國內)RTG_FITCH_NATIONAL_LT (惠譽國內長期評等)
                        //arr[31] (國內)RTG_FITCH_NATIONAL_LT_DT (惠譽國內長期評等日期)
                        //--TRC(中華信評)
                        //arr[32] (國內)RTG_TRC_LONG_TERM (TRC 長期評等)
                        //arr[33] (國內)RTG_TRC_LONG_TERM_RTG_DT (TRC 長期評等日期)
                        if (arr.Length >= 34)
                        {
                            //RTG_SP_LT_LC_ISSUER_CREDIT 標普本國貨幣長期發行人信用評等
                            validateCommpany(
                                arr[8],
                                arr[9],
                                arr[0],
                                A53CommpanyBloombergField.RTG_SP_LT_LC_ISSUER_CREDIT.ToString(),
                                RatingOrg.SP,
                                commpanyData);
                            //RTG_SP_LT_FC_ISSUER_CREDIT 標普長期外幣發行人信用評等
                            validateCommpany(
                                arr[10],
                                arr[11],
                                arr[0],
                                A53CommpanyBloombergField.RTG_SP_LT_FC_ISSUER_CREDIT.ToString(),
                                RatingOrg.SP,
                                commpanyData);
                            //RTG_MDY_LOCAL_LT_BANK_DEPOSITS 穆迪長期本國銀行存款評等
                            validateCommpany(
                                arr[12],
                                arr[13],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_LOCAL_LT_BANK_DEPOSITS.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_MDY_FC_CURR_ISSUER_RATING  穆迪外幣發行人評等
                            validateCommpany(
                                arr[14],
                                arr[15],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_FC_CURR_ISSUER_RATING.ToString(),
                                RatingOrg.Moody,
                                commpanyData
                                );
                            //RTG_MDY_ISSUER  穆迪發行人評等
                            validateCommpany(
                                arr[16],
                                arr[17],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_ISSUER.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_MOODY_LONG_TERM  穆迪長期評等
                            validateCommpany(
                                arr[18],
                                arr[19],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MOODY_LONG_TERM.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_MDY_SEN_UNSECURED_DEBT  穆迪優先無擔保債務評等
                            validateCommpany(
                                arr[20],
                                arr[21],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_SEN_UNSECURED_DEBT.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_FITCH_LT_ISSUER_DEFAULT 惠譽長期發行人違約評等
                            validateCommpany(
                                arr[22],
                                arr[23],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_LT_ISSUER_DEFAULT.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);

                            //RTG_FITCH_LT_FC_ISSUER_DEFAULT  惠譽長期外幣發行人違約評等
                            validateCommpany(
                                arr[24],
                                arr[25],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_LT_FC_ISSUER_DEFAULT.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);
                            //RTG_FITCH_LT_LC_ISSUER_DEFAULT  惠譽長期本國貨幣發行人違約評等
                            validateCommpany(
                                arr[26],
                                arr[27],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_LT_LC_ISSUER_DEFAULT.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);
                            //RTG_FITCH_SEN_UNSECURED  惠譽優先無擔保債務評等
                            validateCommpany(
                                arr[28],
                                arr[29],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_SEN_UNSECURED.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);
                            //RTG_FITCH_NATIONAL_LT 惠譽國內長期評等
                            validateCommpany(
                                arr[30],
                                arr[31],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_NATIONAL_LT.ToString(),
                                RatingOrg.FitchTwn,
                                commpanyData);
                            //RTG_TRC_LONG_TERM  (國內)TRC 長期評等
                            validateCommpany(
                                arr[32],
                                arr[33],
                                arr[0],
                                A53CommpanyBloombergField.RTG_TRC_LONG_TERM.ToString(),
                                RatingOrg.CW,
                                commpanyData);
                        }
                    }
                    if ("START-OF-DATA".Equals(line))
                        flag = true;
                }
            }

            #endregion commpany Data

            #region saveDb
            db.Rating_Info.RemoveRange(
                db.Rating_Info.Where(x => x.Report_Date == reportDateDt));
            db.Rating_Info.AddRange(sampleData);
            db.Rating_Info.AddRange(commpanyData);
            db.Rating_Info_SampleInfo.RemoveRange(
                db.Rating_Info_SampleInfo.Where(x=>x.Report_Date == reportDateDt));
            db.Rating_Info_SampleInfo.AddRange(
                sampleInfos.Select(x => new Rating_Info_SampleInfo()
                {
                    Bond_Number = x.Bond_Number,
                    GUARANTOR_EQY_TICKER = x.GUARANTOR_EQY_TICKER,
                    GUARANTOR_NAME = x.GUARANTOR_NAME,
                    ISSUER_TICKER = x.ISSUER_TICKER,
                    Report_Date = reportDateDt
                }));
            try
            {
                db.SaveChanges();
                db.Dispose();
                log.saveTransferCheck(
                    type,
                    true,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    true,
                    startTime,
                    logPath,
                    MessageType.Success.GetDescription());
                sampleData.AddRange(commpanyData);
                new CompleteEvent().saveDb(reportDateDt, verInt);
            }
            catch (DbUpdateException ex)
            {
                log.saveTransferCheck(
                    type,
                    false,
                    reportDateDt,
                    1,
                    startTime,
                    DateTime.Now);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    $"message: {ex.Message}" +
                    $", inner message {ex.InnerException?.InnerException?.Message}");
            }

            #endregion saveDb
        }

        public class sampleInfo
        {
            public string Bond_Number { get; set; }
            public string GUARANTOR_EQY_TICKER { get; set; }
            public string GUARANTOR_NAME { get; set; }
            public string ISSUER_TICKER { get; set; }
        }

        #region private function

        /// <summary>
        /// 判斷 sample 是否新增
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="bondNumber">債券編號</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="org">評等機構</param>
        /// <param name="sampleData">Sample要新增的資料</param>
        private void validateSample(
            string rating,
            string ratingDate,
            string bondNumber,
            string bloombergField,
            RatingOrg org,
            List<Rating_Info> sampleData
            )
        {
            rating = fr.forRating(rating); //ForMate Rating
            if (!rating.IsNullOrWhiteSpace() &&
                !nullarr.Contains(rating.Trim())) //Sample評等判斷
            {
                sampleData.Add(saveSample(
                    rating,
                    ratingDate,
                    bondNumber,
                    bloombergField,
                    org
                    ));
            }
        }

        /// <summary>
        /// 判斷 commpany 是否新增
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="bondNumber">債券編號</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="org">評等機構</param>
        /// <param name="commpanyData">Commpany要新增的資料</param>
        private void validateCommpany(
            string rating,
            string ratingDate,
            string bondNumber,
            string bloombergField,
            RatingOrg org,
            List<Rating_Info> commpanyData
            )
        {
            rating = fr.forRating(rating); //ForMate Rating
            if (!rating.IsNullOrWhiteSpace() &&
                !nullarr.Contains(rating.Trim())) //Commpany評等判斷
            {
                commpayInfo cInfo = new commpayInfo();
                if (info.TryGetValue(bondNumber.FormatEquity(), out cInfo))
                {
                    cInfo.Bond_Number.ForEach(
                        x => commpanyData.Add(
                            saveCommpany(
                              rating,
                              ratingDate,
                              x,
                              bloombergField,
                              cInfo.Rating_Object,
                              org
                            ))
                        );
                }
            }
        }

        /// <summary>
        /// return new Rating_Info (sample)
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="bondNumber">債券編號</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="org">評等機構</param>
        /// <returns></returns>
        private Rating_Info saveSample(
            string rating,
            string ratingDate,
            string bondNumber,
            string bloombergField,
            RatingOrg org
            )
        {
            return new Rating_Info()
            {
                Bond_Number = bondNumber.Trim(), //債券編號
                Rating_Date = ratingDate.StringToDateTimeN(), //評等時間
                Rating_Object = RatingObject.Bonds.GetDescription(), //評等對象(發行人,債項,保證人)
                Rating = rating.Trim(), //評等內容
                RTG_Bloomberg_Field = bloombergField, //Bloomberg評等欄位名稱
                Rating_Org = org.GetDescription(), //評等機構
                Report_Date = reportDateDt //評估日/報導日
            };
        }

        /// <summary>
        /// return new Rating_Info (commpany)
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="bondNumber">債券編號</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="obj">評等對象</param>
        /// <param name="org">評等機構</param>
        /// <returns></returns>
        private Rating_Info saveCommpany(
            string rating,
            string ratingDate,
            string bondNumber,
            string bloombergField,
            string obj,
            RatingOrg org
            )
        {
            return new Rating_Info()
            {
                Bond_Number = bondNumber.Trim(), //債券編號
                Rating_Date = ratingDate.StringToDateTimeN(), //評等時間
                Rating_Object = obj, //評等對象(發行人,債項,保證人)
                Rating = rating.Trim(), //評等內容
                RTG_Bloomberg_Field = bloombergField, //Bloomberg評等欄位名稱
                Rating_Org = org.GetDescription(), //評等機構
                Report_Date = reportDateDt //評估日/報導日
            };
        }

        #endregion private function
    }
}