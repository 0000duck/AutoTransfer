using AutoTransfer.BondSpreadInfo;
using AutoTransfer.CreateFile;
using AutoTransfer.SFTPConnect;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class A961
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
        private TableType tableType = TableType.A961;
        private string type = TableType.A961.ToString();
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
                foreach (var item in A41Data)
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

                createA9611File();
            }
        }

        protected void createA9611File()
        {
            if (new CreateA9611File().create(tableType, reportDateStr, verInt))
            {
                putSFTP("1", setFile.putA9611FilePath(), setFile.putA9611FileName());
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

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                     filePath,
                     replyFileName,
                     out error);

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
                Thread.Sleep(20 * 60 * 1000);

                string getFilePath = "";
                string getFileName = "";

                switch (fileNumber)
                {
                    case "1":
                        getFilePath = setFile.getA9611FilePath();
                        getFileName = setFile.getA9611GZFileName();
                        break;
                    case "2":
                        getFilePath = setFile.getA9612FilePath();
                        getFileName = setFile.getA9612FileName();
                        break;
                    case "3":
                        getFilePath = setFile.getA9613FilePath();
                        getFileName = setFile.getA9613FileName();
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

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Get(string.Empty,
                     getFilePath,
                     getFileName,
                     out error);

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
                string sourceFileName = "";
                string destFileName = "";

                switch (fileNumber)
                {
                    case "1":
                        sourceFileName = Path.Combine(getFilePath, getFileName);
                        destFileName = Path.Combine(getFilePath, setFile.getA9611FileName());
                        Extension.Decompress(sourceFileName, destFileName);
                        DataMidYieldToA96(setFile.getA9611FilePath(), setFile.getA9611FileName());
                        createA9612File();
                        break;
                    case "2":
                        createA9613File();
                        break;
                    case "3":
                        DataID_CUSIPToA96();
                        DataToDb();
                        break;
                    default:
                        break;
                }
            }
        }

        protected void createA9612File()
        {
            if (new CreateA9612File().create(tableType, reportDateStr, verInt))
            {
                putSFTP("2", setFile.putA9612FilePath(), setFile.putA9612FileName());
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

        protected void DataMidYieldToA96(string path1, string path2)
        {
            IFRS9Entities db = new IFRS9Entities();

            #region
            using (StreamReader sr = new StreamReader(Path.Combine(path1, path2)))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;
                List<YLD_YTM_MID_Class> listYLD = new List<YLD_YTM_MID_Class>();
                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                    {
                        flag = false;
                    }

                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        //arr[0]  ex: US035242AN64@BGN Govt
                        //arr[1]  ex: 0 or 10 ??
                        //arr[2]  ex: 1 ??
                        //arr[3]  ex: 01/31/2018 
                        //arr[4]  ex: 4.097 --Mid_Yield

                        if (arr.Length >= 5 && !arr[0].IsNullOrWhiteSpace() &&
                            !arr[0].StartsWith("START") && !arr[0].StartsWith("END"))
                        {
                            //double YLD_YTM_MID;
                            double BVAL_MID_YTM;
                            if (!arr[4].IsNullOrWhiteSpace() &&
                                double.TryParse(arr[4].Trim(), out BVAL_MID_YTM))
                            {
                                var Bond_Number = arr[0].Trim().Split('@')[0];

                                A96Data.Where(x => x.Bond_Number == Bond_Number)
                                       .ToList()
                                       .ForEach(x =>
                                       {
                                           x.Mid_Yield = BVAL_MID_YTM.ToString();
                                       });
                            }
                        }
                    }

                    if ("START-OF-DATA".Equals(line))
                    {
                        flag = true;
                    }
                }
            }
            #endregion
        }

        protected void createA9613File()
        {
            try
            {
                List<string> data = new List<string>();
                using (StreamReader sr = new StreamReader(Path.Combine(setFile.getA9612FilePath(), setFile.getA9612FileName())))
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

                                A96Data.Where(x => x.Bond_Number == bondNumber)
                                       .ToList()
                                       .ForEach(x =>
                                       {
                                           x.BNCHMRK_TSY_ISSUE_ID = BNCHMRK_TSY_ISSUE_ID;
                                       });

                                if (BNCHMRK_TSY_ISSUE_ID != ""
                                    && BNCHMRK_TSY_ISSUE_ID != "N.A."
                                    && BNCHMRK_TSY_ISSUE_ID != "Govt")
                                {
                                    data.Add(BNCHMRK_TSY_ISSUE_ID);
                                }
                            }
                        }

                        if ("START-OF-DATA".Equals(line))
                        {
                            flag = true;
                        }
                    }

                    if (new CreateA9613File().create(tableType, reportDateStr, data))
                    {
                        putSFTP("3", setFile.putA9613FilePath(), setFile.putA9613FileName());
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

        protected void DataID_CUSIPToA96()
        {
            try
            {
                List<string> data = new List<string>();
                using (StreamReader sr = new StreamReader(Path.Combine(setFile.getA9613FilePath(), setFile.getA9613FileName())))
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
                                var ID_CUSIP = arr[3].Trim();

                                if (ID_CUSIP != "" && ID_CUSIP != "N.A.")
                                {
                                    A96Data.Where(x => x.BNCHMRK_TSY_ISSUE_ID == BNCHMRK_TSY_ISSUE_ID)
                                           .ToList()
                                           .ForEach(x =>
                                           {
                                               x.ID_CUSIP = ID_CUSIP;
                                           });
                                }
                            }
                        }

                        if ("START-OF-DATA".Equals(line))
                        {
                            flag = true;
                        }
                    }
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

        protected void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();

            #region saveDb
            try
            {
                db.Bond_Spread_Info.RemoveRange(db.Bond_Spread_Info.Where(x => x.Report_Date == reportDateDt));

                db.Bond_Spread_Info.AddRange(A96Data.Select(
                                                               x => new Bond_Spread_Info()
                                                               {
                                                                   Reference_Nbr = x.Reference_Nbr,
                                                                   Report_Date = DateTime.Parse(x.Report_Date),
                                                                   Version = int.Parse(x.Version),
                                                                   Bond_Number = x.Bond_Number,
                                                                   Lots = x.Lots,
                                                                   Portfolio_Name = x.Portfolio_Name,
                                                                   Origination_Date = DateTime.Parse(x.Origination_Date),
                                                                   EIR = double.Parse(x.EIR),
                                                                   Processing_Date = DateTime.Parse(x.Processing_Date),
                                                                   Mid_Yield = TypeTransfer.stringToDoubleN(x.Mid_Yield),
                                                                   BNCHMRK_TSY_ISSUE_ID = x.BNCHMRK_TSY_ISSUE_ID,
                                                                   ID_CUSIP = x.ID_CUSIP,
                                                                   Spread_Current = TypeTransfer.stringToDoubleN(x.Spread_Current),
                                                                   Spread_When_Trade = TypeTransfer.stringToDoubleN(x.Spread_When_Trade),
                                                                   Treasury_Current = TypeTransfer.stringToDoubleN(x.Treasury_Current),
                                                                   Treasury_When_Trade = TypeTransfer.stringToDoubleN(x.Treasury_When_Trade),
                                                                   All_in_Chg = TypeTransfer.stringToDoubleN(x.All_in_Chg),
                                                                   Chg_In_Spread = TypeTransfer.stringToDoubleN(x.Chg_In_Spread),
                                                                   Chg_In_Treasury = TypeTransfer.stringToDoubleN(x.Chg_In_Treasury)
                                                               }
                                                           )
                                            );
                db.SaveChanges();
                db.Dispose();

                #region
                log.bothLog(
                    type,
                    true,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    MessageType.Success.GetDescription()
                    );
                #endregion
            }
            catch (Exception ex)
            {
                #region
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
                #endregion
            }
            #endregion saveDb
        }

        public class YLD_YTM_MID_Class
        {
            public string ID_CUSIP { get; set; }
            public string DATE_YLD_YTM_MID { get; set; }
            public string YLD_YTM_MID { get; set; }
        }
    }
}