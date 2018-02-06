using AutoTransfer.Abstract;
using AutoTransfer.Bond_Spread_Info;
using AutoTransfer.Commpany;
using AutoTransfer.CreateFile;
using AutoTransfer.Sample;
using AutoTransfer.SFTPConnect;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class A96
    {
        #region 共用參數
        private FormatRating fr = new FormatRating();

        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private int verInt = 0;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A96;
        private string type = TableType.A96.ToString();
        public List<A96_Bond_Spread_Info> A96Data = new List<A96_Bond_Spread_Info>();
        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public void startTransfer(string dateTime)
        {
            logPath = log.txtLocation(type);

            IFRS9Entities db = new IFRS9Entities();
            startTime = DateTime.Now;

            if (dateTime.Length != 8 ||
               !DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
               System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt))
            {
                db.Dispose();
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    MessageType.DateTime_Format_Fail.GetDescription()
                    );
            }

            var A41 = db.Bond_Account_Info.AsNoTracking()
                        .Any(x => x.Report_Date == reportDateDt);
            verInt = db.Bond_Account_Info.AsNoTracking()
                       .Where(x => x.Report_Date == reportDateDt && x.Version != null)
                       .DefaultIfEmpty().Max(x => x.Version == null ? 0 : x.Version.Value);
            
            if (!A41 || verInt == 0)
            {
                db.Dispose();
                List<string> errs = new List<string>();

                if (!A41)
                {
                    errs.Add(MessageType.not_Find_Any.GetDescription("A41"));
                }

                if (verInt == 0)
                {
                    errs.Add(MessageType.transferError.GetDescription());
                }

                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    string.Join(",", errs)
                    );
            }
            else
            {
                List<Bond_Account_Info> A41Data = db.Bond_Account_Info.AsNoTracking()
                                                    .Where(x => x.Report_Date == reportDateDt
                                                             && x.Version == verInt).ToList();
                foreach(var item in A41Data)
                {
                    A96_Bond_Spread_Info A96One = new A96_Bond_Spread_Info();
                    A96One.Reference_Nbr = item.Reference_Nbr;
                    A96One.Report_Date = TypeTransfer.dateTimeNToString(item.Report_Date);
                    A96One.Version = TypeTransfer.intNToString(item.Version);
                    A96One.Bond_Number = item.Bond_Number;
                    A96One.Lots = item.Lots;
                    A96One.Portfolio_Name = item.Portfolio_Name;
                    A96One.Origination_Date = TypeTransfer.dateTimeNToString(item.Origination_Date);
                    A96One.EIR = TypeTransfer.doubleNToString(item.EIR);
                    A96One.Processing_Date = DateTime.Now.ToString("yyyy/MM/dd");

                    A96Data.Add(A96One);
                }

                db.Dispose();

                reportDateStr = dateTime;
                setFile = new SetFile(tableType, dateTime);

                createA96_1File();
            }
        }

        protected void createA96_1File()
        {
            if (new CreateA96_1File().create(tableType, reportDateStr, verInt))
            {
                //把資料送給SFTP
                putA96_1SFTP();
            }
            else
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    MessageType.Create_File_Fail.GetDescription(type)
                );
            }
        }

        protected void putA96_1SFTP()
        {
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Put(string.Empty,
            //     setFile.putA96_1FilePath(),
            //     setFile.putA96_1FileName(),
            //     out error);

            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    error
                );
            }
            else
            {
                //Thread.Sleep(20 * 60 * 1000);
                getA96_1SFTP();
            }
        }

        protected void getA96_1SFTP()
        {
            new FileRelated().createFile(setFile.getA96_1FilePath());
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Get(string.Empty,
            //    setFile.getA96_1FilePath(),
            //    setFile.getA96_1FileName(),
            //    out error);

            if (!error.IsNullOrWhiteSpace())
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    "下載A96_1檔案失敗"
                    );
            }
            else
            {
                createA96_2File();
            }
        }

        protected void createA96_2File()
        {
            List<string> data = new List<string>();
            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getA96_1FilePath(), setFile.getA96_1FileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                    {
                        flag = false;
                    }

                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        //arr[0]  ex: US65504LAK35 Corp
                        //arr[1]  ex: 0
                        //arr[2]  ex: 1
                        //arr[3]  ex: 912810QS Govt
                        if (arr.Length >= 4)
                        {
                            var bondNumber = arr[0].Trim().Split(' ')[0];

                            A96Data.Where(x=>x.Bond_Number == bondNumber)
                                   .ToList()
                                   .ForEach(x =>
                                   {
                                       x.BNCHMRK_TSY_ISSUE_ID = arr[3].Trim();
                                   });

                            data.Add(arr[3].Trim());
                        }
                    }

                    if ("START-OF-DATA".Equals(line))
                    {
                        flag = true;
                    }
                }

                try
                {
                    if (new CreateA96_2File().create(tableType, reportDateStr, data))
                    {
                        putA96_2SFTP();
                    }
                    else
                    {
                        log.bothLog(
                            type,
                            false,
                            reportDateDt,
                            startTime,
                            DateTime.Now,
                            1,
                            logPath,
                            MessageType.Create_File_Fail.GetDescription()
                            );
                    }
                }
                catch (Exception ex)
                {
                    log.bothLog(
                        type,
                        false,
                        reportDateDt,
                        startTime,
                        DateTime.Now,
                        1,
                        logPath,
                        $"message: {ex.Message}" +
                        $", inner message {ex.InnerException?.InnerException?.Message}"
                        );
                }
            }
        }

        protected void putA96_2SFTP()
        {
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Put(string.Empty,
            //     setFile.putA96_2FilePath(),
            //     setFile.putA96_2FileName(),
            //     out error);

            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    error
                );
            }
            else
            {
                //Thread.Sleep(20 * 60 * 1000);
                getA96_2SFTP();
            }
        }

        protected void getA96_2SFTP()
        {
            new FileRelated().createFile(setFile.getA96_2FilePath());
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Get(string.Empty,
            //    setFile.getA96_2FilePath(),
            //    setFile.getA96_2FileName(),
            //    out error);

            if (!error.IsNullOrWhiteSpace())
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    "下載A96_2檔案失敗"
                    );
            }
            else
            {
                createA96_3File();
            }
        }

        protected void createA96_3File()
        {
            List<string> data = new List<string>();
            using (StreamReader sr = new StreamReader(Path.Combine(
                   setFile.getA96_2FilePath(), setFile.getA96_2FileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                    {
                        flag = false;
                    }

                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        //arr[0]  ex: 912810QS Govt
                        //arr[1]  ex: 0
                        //arr[2]  ex: 1
                        //arr[3]  ex: 912810QS0
                        if (arr.Length >= 4)
                        {
                            var BNCHMRK_TSY_ISSUE_ID = arr[0].Trim();

                            A96Data.Where(x => x.BNCHMRK_TSY_ISSUE_ID == BNCHMRK_TSY_ISSUE_ID)
                                   .ToList()
                                   .ForEach(x =>
                                   {
                                       x.ID_CUSIP = arr[3].Trim();
                                   });

                            data.Add(arr[3].Trim());
                        }
                    }

                    if ("START-OF-DATA".Equals(line))
                    {
                        flag = true;
                    }
                }

                try
                {
                    if (new CreateA96_3File().create(tableType, reportDateStr, data))
                    {
                        putA96_3SFTP();
                    }
                    else
                    {
                        log.bothLog(
                            type,
                            false,
                            reportDateDt,
                            startTime,
                            DateTime.Now,
                            1,
                            logPath,
                            MessageType.Create_File_Fail.GetDescription()
                            );
                    }
                }
                catch (Exception ex)
                {
                    log.bothLog(
                        type,
                        false,
                        reportDateDt,
                        startTime,
                        DateTime.Now,
                        1,
                        logPath,
                        $"message: {ex.Message}" +
                        $", inner message {ex.InnerException?.InnerException?.Message}"
                        );
                }
            }
        }

        protected void putA96_3SFTP()
        {
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Put(string.Empty,
            //     setFile.putA96_3FilePath(),
            //     setFile.putA96_3FileName(),
            //     out error);

            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    error
                );
            }
            else
            {
                //Thread.Sleep(20 * 60 * 1000);
                getA96_3SFTP();
            }
        }

        protected void getA96_3SFTP()
        {
            new FileRelated().createFile(setFile.getA96_3FilePath());
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Get(string.Empty,
            //    setFile.getA96_3FilePath(),
            //    setFile.getA96_3FileName(),
            //    out error);

            if (!error.IsNullOrWhiteSpace())
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    "下載A96_3檔案失敗"
                    );
            }
            else
            {
                //DataToDb();
            }
        }
    }
}
