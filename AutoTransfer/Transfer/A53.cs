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
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class A53 : SFTPTransfer
    {
        #region 共用參數
        private TableType tableType = TableType.A53;
        private string type = TableType.A53.ToString();
        private string reportDateStr = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private DateTime startTime = DateTime.MinValue;
        private Dictionary<string, commpayInfo> info =
            new Dictionary<string, commpayInfo>();
        private SetFile setFile = null;
        private Log log = new Log();
        private string logPath = string.Empty;
        #endregion

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public override void startTransfer(string dateTime)
        {
            startTime = DateTime.Now;
            logPath = log.txtLocation(type);
            if (dateTime.Length != 8 ||
               !DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
               System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt))
            {
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    false);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.DateTime_Format_Fail.GetDescription());
            }
            else
            {
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
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    false);
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
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    false);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Put_Sample_File_Fail.GetDescription());
            }
            else //success (wait 20 min and get data)
            {
                ThreadTask t = new ThreadTask();
                Action f = () => getSampleSFTP();
                t.Start(f); //委派 設定時間後要做的動作
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
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    false);
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
                        if (arr.Length >= 10)
                        {
                            if (!arr[3].IsNullOrWhiteSpace() &&
                                !nullarr.Contains(arr[3].Trim()))  //擔保人不是空值
                            {
                                commpayInfo x = new commpayInfo(); 
                                if (!info.TryGetValue(arr[3].Trim(), out x))
                                {
                                    data.Add(arr[3].Trim());
                                    info.Add(arr[3].Trim(), new commpayInfo()
                                    {
                                        Bond_Number = arr[0].Trim(),
                                        Rating_Object = RatingObject.GUARANTOR.GetDescription()
                                    });
                                }
                            }
                            if (!arr[7].Trim().IsNullOrWhiteSpace() &&
                                !nullarr.Contains(arr[7].Trim()))  //發行人不是空值
                            {
                                commpayInfo x = new commpayInfo();
                                if (!info.TryGetValue(arr[7].Trim(), out x))
                                {
                                    data.Add(arr[7].Trim());
                                    info.Add(arr[7].Trim(), new commpayInfo()
                                    {
                                        Bond_Number = arr[0].Trim(),
                                        Rating_Object = RatingObject.ISSUER.GetDescription()
                                    });
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
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    false);
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
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    false);
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Put_Commpany_File_Fail.GetDescription());
            }
            else //success (wait 20 min and get data)
            {
                ThreadTask t = new ThreadTask();
                Action f = () => getCommpanySFTP();
                t.Start(f); //委派 設定時間後要做的動作
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
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    false);
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
            List<Rating_SP_Info> sampleData = new List<Rating_SP_Info>();
            List<Rating_SP_Info> commpanyData = new List<Rating_SP_Info>();
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
                        //arr[0] ex: US00206RDH21 (債券編號)
                        //arr[1] ex: 0
                        //arr[2] ex: 20
                        //arr[3] ex: N.A. (GUARANTOR_EQY_TICKER 擔保人)
                        //arr[4] ex: Subsidiaries (GUARANTOR_NAME 擔保人名稱)
                        //arr[5] ex: 03/17/2016 (ISSUE_DT 債券評等日期)
                        //arr[6] ex: AT&T INC (ISSUER 債券名稱)
                        //arr[7] ex: T US (ISSUER_EQUITY_TICKER 發行人)
                        //arr[8] ex: N.S. (RTG_SP S&P國外評等)
                        //arr[9] ex: N.S. (SP_EFF_DT S&P國外評等日期)
                        if (arr.Length >= 10)
                        {
                            if (!arr[8].IsNullOrWhiteSpace() &&
                                !nullarr.Contains(arr[8].Trim())) //S&P國外評等
                            {
                                sampleData.Add(new Rating_SP_Info()
                                {
                                    Bond_Number = arr[0].Trim(), //債券編號
                                    Rating_Date = arr[9].StringToDateTimeN(),
                                    Rating_Object = RatingObject.Bonds.GetDescription(), //評等對象(發行人,債項,保證人)
                                    Rating = arr[8].Trim(), //評等內容
                                    RTG_Bloomberg_Field = a53Sample.RTG_SP.ToString() //Bloomberg評等欄位名稱
                                });
                            }
                        }
                    }
                    if ("START-OF-DATA".Equals(line))
                        flag = true;
                }
            }
            #endregion
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
                        //arr[0] ex: T US Equity (發行人or擔保人)
                        //arr[1] ex: 0
                        //arr[2] ex: 38
                        //arr[3] ex: US (城市(國家)) 
                        //arr[4] ex: 101376 (公司ID) 
                        //arr[5] ex: Telecommunications (INDUSTRY_GROUP)
                        //arr[6] ex: Communications (INDUSTRY_SECTOR)
                        //arr[7] ex: AT&T Inc (公司名稱)
                        //arr[8] ex: N.S. (RTG_SP_LT_FC_ISS_CRED_RTG_DT 標普長期外幣發行人信用評等日期) 
                        //arr[9] ex: N.S. (RTG_SP_LT_FC_ISSUER_CREDIT 標普長期外幣發行人信用評等)                      
                        //arr[10] ex: N.S. (RTG_SP_LT_LC_ISS_CRED_RTG_DT (國內)標普本國貨幣長期發行人信用評等日期)
                        //arr[11] ex: N.S. (RTG_SP_LT_LC_ISSUER_CREDIT (國內)標普本國貨幣長期發行人信用評等)
                        if(arr.Length >= 12)
                        {
                            if (!arr[9].IsNullOrWhiteSpace() &&
                                !nullarr.Contains(arr[9].Trim())) //標普長期外幣發行人信用評等
                            {
                                commpayInfo cInfo = new commpayInfo();
                                if (info.TryGetValue(arr[0].FormatEquity(), out cInfo))
                                {
                                    commpanyData.Add(new Rating_SP_Info()
                                    {
                                        Bond_Number = cInfo.Bond_Number, //債券編號
                                        Rating_Date = arr[8].StringToDateTimeN(), //評等資料時點
                                        Rating_Object = cInfo.Rating_Object, //評等對象(發行人,債項,保證人)
                                        Rating = arr[9].Trim(), //評等內容
                                        RTG_Bloomberg_Field =  a53Commpany.RTG_SP_LT_FC_ISSUER_CREDIT.ToString() //Bloomberg評等欄位名稱
                                    });
                                }
                            }
                            if (!arr[11].IsNullOrWhiteSpace() &&
                                !nullarr.Contains(arr[11].Trim())) //標普本國貨幣長期發行人信用評等
                            {
                                commpayInfo cInfo = new commpayInfo();
                                if (info.TryGetValue(arr[0].FormatEquity(), out cInfo))
                                {
                                    commpanyData.Add(new Rating_SP_Info()
                                    {
                                        Bond_Number = cInfo.Bond_Number, //債券編號
                                        Rating_Date = arr[10].StringToDateTimeN(), //評等資料時點
                                        Rating_Object = cInfo.Rating_Object, //評等對象(發行人,債項,保證人)
                                        Rating = arr[11].Trim(), //評等內容
                                        RTG_Bloomberg_Field = a53Commpany.RTG_SP_LT_LC_ISSUER_CREDIT.ToString()  //Bloomberg評等欄位名稱
                                    });
                                }
                            }
                        }
                    }
                    if ("START-OF-DATA".Equals(line))
                        flag = true;
                }
            }
            #endregion
            #region saveDb
            db.Rating_SP_Info.AddRange(sampleData);
            db.Rating_SP_Info.AddRange(commpanyData);
            try
            {
                //db.SaveChanges();
                db.Dispose();
                log.sqlLog(
                    type,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    true);
                log.txtLog(
                    type,
                    true,
                    startTime,
                    logPath,
                    MessageType.Success.GetDescription());
                new CompleteEvent().trigger(reportDateDt);
            }
            catch (DbUpdateException ex)
                {
                    
                    log.sqlLog(
                        type,
                        reportDateDt,
                        startTime,
                        DateTime.Now,
                        false);
                    log.txtLog(
                        type,
                        false,
                        startTime,
                        logPath,
                        $"message: {ex.Message}" +
                        $", inner message {ex.InnerException?.InnerException?.Message}");
                }
            #endregion          
        }
    }
}
