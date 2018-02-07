using AutoTransfer.Abstract;
using AutoTransfer.BondSpreadInfo;
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
                putSFTP("1", setFile.putA96_1FilePath(), setFile.putA96_1FileName());
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

        protected void putSFTP(string fileNumber, string filePath, string replyFileName)
        {
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Put(string.Empty,
            //         filePath,
            //         replyFileName,
            //         out error);

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
                string getFilePath = "";
                string getFileName = "";

                switch (fileNumber)
                {
                    case "1":
                        getFilePath = setFile.getA96_1FilePath();
                        getFileName = setFile.getA96_1FileName();
                        break;
                    case "2":
                        getFilePath = setFile.getA96_2FilePath();
                        getFileName = setFile.getA96_2FileName();
                        break;
                    case "3":
                        getFilePath = setFile.getA96_3FilePath();
                        getFileName = setFile.getA96_3FileName();
                        break;
                    default:
                        break;
                }

                getSFTP(fileNumber, getFilePath, getFileName);
            }
        }

        protected void getSFTP(string fileNumber, string getFilePath, string getFileName)
        {
            new FileRelated().createFile(getFilePath);

            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Get(string.Empty,
            //         getFilePath,
            //         getFileName,
            //         out error);

            if (!error.IsNullOrWhiteSpace())
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
                switch (fileNumber)
                {
                    case "1":
                        createA96_2File();
                        break;
                    case "2":
                        createA96_3File();
                        break;
                    case "3":
                        //DataToDb();
                        break;
                    default:
                        break;
                }
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
                            var BNCHMRK_TSY_ISSUE_ID = arr[3].Trim();

                            if (BNCHMRK_TSY_ISSUE_ID != "" 
                                && BNCHMRK_TSY_ISSUE_ID != "N.A." 
                                && BNCHMRK_TSY_ISSUE_ID != "Govt")
                            {
                                A96Data.Where(x => x.Bond_Number == bondNumber)
                                       .ToList()
                                       .ForEach(x =>
                                       {
                                           x.BNCHMRK_TSY_ISSUE_ID = BNCHMRK_TSY_ISSUE_ID;
                                       });

                                data.Add(BNCHMRK_TSY_ISSUE_ID);
                            }
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
                        putSFTP("2", setFile.putA96_2FilePath(), setFile.putA96_2FileName());
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
                        verInt,
                        logPath,
                        $"message: {ex.Message}" +
                        $", inner message {ex.InnerException?.InnerException?.Message}"
                        );
                }
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
                    string minOriginationDate = A96Data.DefaultIfEmpty()
                                                       .Max(x => x.Origination_Date.IsNullOrWhiteSpace() == true ? reportDateStr : x.Origination_Date);

                    if (new CreateA96_3File().create(tableType, DateTime.Parse(minOriginationDate).ToString("yyyy/MM/dd") ,reportDateStr, data))
                    {
                        putSFTP("3", setFile.putA96_3FilePath(), setFile.putA96_3FileName());
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
                        verInt,
                        logPath,
                        $"message: {ex.Message}" +
                        $", inner message {ex.InnerException?.InnerException?.Message}"
                        );
                }
            }
        }

        protected void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();

            #region
            using (StreamReader sr = new StreamReader(Path.Combine(
                   setFile.getA96_3FilePath(), setFile.getA96_3FileName())))
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
                        //arr[0]  ex: 912810QS0@BGN Govt
                        //arr[1]  ex: 03/22/2013
                        //arr[2]  ex: 3.089

                        if (arr.Length >= 3 && !arr[0].IsNullOrWhiteSpace() &&
                            !arr[0].StartsWith("START") && !arr[0].StartsWith("END"))
                        {
                            var ID_CUSIP = arr[0].Trim().Split('@')[0];

                            A96Data.Where(x => x.ID_CUSIP == ID_CUSIP)
                                   .ToList()
                                   .ForEach(x =>
                                   {
                                       x.ID_CUSIP = arr[3].Trim();
                                   });
                        }
                    }

                    if ("START-OF-DATA".Equals(line))
                    {
                        flag = true;
                    }
                }
            }
            #endregion

            #region saveDb
            try
            {
                //db.Econ_Domestic.AddRange(A07Datas);
                //db.SaveChanges();
                //db.Dispose();
                //#region 加入 sql transferCheck by Mark 2018/01/09
                //log.bothLog(
                //    type,
                //    true,
                //    reportDateDt,
                //    startTime,
                //    DateTime.Now,
                //    1,
                //    logPath,
                //    MessageType.Success.GetDescription()
                //    );
                //#endregion
            }
            catch (Exception ex)
            {
                #region 加入 sql transferCheck by Mark 2018/01/09
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
                #endregion
            }
            #endregion saveDb
        }
    }
}