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
                        getFileName = setFile.getA96_1GZFileName();
                        break;
                    case "2":
                        getFilePath = setFile.getA96_2FilePath();
                        getFileName = setFile.getA96_2FileName();
                        break;
                    case "3":
                        getFilePath = setFile.getA96_3FilePath();
                        getFileName = setFile.getA96_3FileName();
                        break;
                    case "4":
                        getFilePath = setFile.getA96_4FilePath();
                        getFileName = setFile.getA96_4GZFileName();
                        break;
                    case "5":
                        getFilePath = setFile.getA96_5FilePath();
                        getFileName = setFile.getA96_5GZFileName();
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
                string sourceFileName = "";
                string destFileName = "";

                switch (fileNumber)
                {
                    case "1":
                        sourceFileName = Path.Combine(getFilePath, getFileName);
                        destFileName = Path.Combine(getFilePath, setFile.getA96_1FileName());
                        Extension.Decompress(sourceFileName, destFileName);
                        DataMidYieldToA96(setFile.getA96_1FilePath(), setFile.getA96_1FileName());
                        createA96_2File();
                        break;
                    case "2":
                        createA96_3File();
                        break;
                    case "3":
                        createA96_4File();
                        break;
                    case "4":
                        sourceFileName = Path.Combine(getFilePath, getFileName);
                        destFileName = Path.Combine(getFilePath, setFile.getA96_4FileName());
                        Extension.Decompress(sourceFileName, destFileName);
                        DataToA96(setFile.getA96_4FilePath(), setFile.getA96_4FileName());
                        createA96_5File();
                        break;
                    case "5":
                        //sourceFileName = Path.Combine(getFilePath, getFileName);
                        //destFileName = Path.Combine(getFilePath, setFile.getA96_5FileName());
                        //Extension.Decompress(sourceFileName, destFileName);
                        //DataToA96(setFile.getA96_5FilePath(), setFile.getA96_5FileName());
                        DataToDb();
                        break;
                    default:
                        break;
                }
            }
        }

        protected void createA96_2File()
        {
            if (new CreateA96_2File().create(tableType, reportDateStr, verInt))
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
                    MessageType.Create_File_Fail.GetDescription(type)
                );
            }
        }

        protected void DataMidYieldToA96(string path1, string path2)
        {
            //IFRS9Entities db = new IFRS9Entities();

            //#region
            //using (StreamReader sr = new StreamReader(Path.Combine(path1, path2)))
            //{
            //    bool flag = false; //判斷是否為要讀取的資料行數
            //    string line = string.Empty;
            //    List<YLD_YTM_MID_Class> listYLD = new List<YLD_YTM_MID_Class>();
            //    while ((line = sr.ReadLine()) != null)
            //    {
            //        if ("END-OF-DATA".Equals(line))
            //        {
            //            flag = false;
            //        }

            //        if (flag) //找到的資料
            //        {
            //            var arr = line.Split('|');
            //            //arr[0]  ex: 912810QS0@BGN Govt
            //            //arr[1]  ex: 03/22/2013
            //            //arr[2]  ex: 3.089

            //            if (arr.Length >= 3 && !arr[0].IsNullOrWhiteSpace() &&
            //                !arr[0].StartsWith("START") && !arr[0].StartsWith("END"))
            //            {
            //                var ID_CUSIP = arr[0].Trim().Split('@')[0];
            //                var dateYLD_YTM_MID = arr[1].Trim();
            //                dateYLD_YTM_MID = dateYLD_YTM_MID.Substring(6, 4) + "/" + dateYLD_YTM_MID.Substring(0, 2) + "/" + dateYLD_YTM_MID.Substring(3, 2);
            //                var YLD_YTM_MID = arr[2].Trim();

            //                YLD_YTM_MID_Class yld = new YLD_YTM_MID_Class();
            //                yld.ID_CUSIP = ID_CUSIP;
            //                yld.DATE_YLD_YTM_MID = dateYLD_YTM_MID;
            //                yld.YLD_YTM_MID = YLD_YTM_MID;
            //                listYLD.Add(yld);
            //            }
            //        }

            //        if ("START-OF-DATA".Equals(line))
            //        {
            //            flag = true;
            //        }
            //    }

            //    listYLD = listYLD.GroupBy(o => new { o.ID_CUSIP, o.DATE_YLD_YTM_MID, o.YLD_YTM_MID })
            //                     .Select(o => o.FirstOrDefault()).ToList();

            //    foreach (var item in A96Data)
            //    {
            //        var YLDs = listYLD.Where(x => x.ID_CUSIP == item.ID_CUSIP
            //                                    && (x.DATE_YLD_YTM_MID == item.Report_Date || x.DATE_YLD_YTM_MID == item.Origination_Date)).ToList();

            //        foreach (var oneYLD in YLDs)
            //        {
            //            item.Mid_Yield = oneYLD.YLD_YTM_MID;

            //            if (item.Mid_Yield != null && item.Mid_Yield != "")
            //            {
            //                if (item.Report_Date == oneYLD.DATE_YLD_YTM_MID)
            //                {
            //                    item.Treasury_Current = (double.Parse(item.Mid_Yield) * 100).ToString();
            //                }

            //                if (item.Origination_Date == oneYLD.DATE_YLD_YTM_MID)
            //                {
            //                    item.Treasury_When_Trade = (double.Parse(item.Mid_Yield) * 100).ToString();
            //                }

            //                if (item.Treasury_Current != null && item.Treasury_Current != "")
            //                {
            //                    item.Spread_Current = (double.Parse(item.Mid_Yield) * 100 - double.Parse(item.Treasury_Current)).ToString();
            //                }

            //                if (item.Treasury_When_Trade != null && item.Treasury_When_Trade != "")
            //                {
            //                    item.Spread_When_Trade = (double.Parse(item.EIR) - double.Parse(item.Treasury_When_Trade)).ToString();
            //                }

            //                if (item.Spread_Current.IsNullOrWhiteSpace() == false
            //                    && item.Spread_When_Trade.IsNullOrWhiteSpace() == false
            //                    && item.Treasury_Current.IsNullOrWhiteSpace() == false
            //                    && item.Treasury_When_Trade.IsNullOrWhiteSpace() == false
            //                   )
            //                {
            //                    item.All_in_Chg = (double.Parse(item.Spread_Current)
            //                                       - double.Parse(item.Spread_When_Trade)
            //                                       + double.Parse(item.Treasury_Current)
            //                                       - double.Parse(item.Treasury_When_Trade)).ToString();
            //                }

            //                if (item.Spread_Current.IsNullOrWhiteSpace() == false
            //                    && item.Spread_When_Trade.IsNullOrWhiteSpace() == false
            //                   )
            //                {
            //                    item.Chg_In_Spread = (double.Parse(item.Spread_Current) - double.Parse(item.Spread_When_Trade)).ToString();
            //                }

            //                if (item.Treasury_Current.IsNullOrWhiteSpace() == false
            //                    && item.Treasury_When_Trade.IsNullOrWhiteSpace() == false)
            //                {
            //                    item.Chg_In_Treasury = (double.Parse(item.Treasury_Current) - double.Parse(item.Treasury_When_Trade)).ToString();
            //                }
            //            }
            //        }
            //    }
            //}
            //#endregion
        }

        protected void createA96_3File()
        {
            try
            {
                List<string> data = new List<string>();
                using (StreamReader sr = new StreamReader(Path.Combine(setFile.getA96_2FilePath(), setFile.getA96_2FileName())))
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

                    //A96Data.RemoveAll(x => x.BNCHMRK_TSY_ISSUE_ID == null || x.BNCHMRK_TSY_ISSUE_ID == "");

                    if (new CreateA96_3File().create(tableType, reportDateStr, data))
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

        protected void createA96_4File()
        {
            try
            {
                List<string> data = new List<string>();
                using (StreamReader sr = new StreamReader(Path.Combine(setFile.getA96_3FilePath(), setFile.getA96_3FileName())))
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

                                    data.Add(ID_CUSIP);
                                }
                            }
                        }

                        if ("START-OF-DATA".Equals(line))
                        {
                            flag = true;
                        }
                    }

                    //A96Data.RemoveAll(x => x.ID_CUSIP == null || x.ID_CUSIP == "");

                    if (new CreateA96_4File().create(tableType, reportDateStr, data))
                    {
                        putSFTP("4", setFile.putA96_4FilePath(), setFile.putA96_4FileName());
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

        protected void createA96_5File()
        {
            try
            {
                List<string> data = new List<string>();
                using (StreamReader sr = new StreamReader(Path.Combine(setFile.getA96_3FilePath(), setFile.getA96_3FileName())))
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

                                    data.Add(ID_CUSIP);
                                }
                            }
                        }

                        if ("START-OF-DATA".Equals(line))
                        {
                            flag = true;
                        }
                    }

                    //A96Data.RemoveAll(x => x.ID_CUSIP == null || x.ID_CUSIP == "");

                    string minOriginationDate = A96Data.DefaultIfEmpty()
                                                       .Min(x => x.Origination_Date.IsNullOrWhiteSpace() == true ? reportDateStr : x.Origination_Date);
                    string maxOriginationDate = A96Data.DefaultIfEmpty()
                                                       .Max(x => x.Origination_Date.IsNullOrWhiteSpace() == true ? reportDateStr : x.Origination_Date);
                    List<string> dateRange = new List<string>();
                    dateRange.Add(DateTime.Parse(minOriginationDate).ToString("yyyyMMdd"));
                    dateRange.Add(DateTime.Parse(maxOriginationDate).ToString("yyyyMMdd"));
                    dateRange.Sort();

                    if (new CreateA96_5File().create(tableType, dateRange[0], dateRange[1], reportDateStr, data))
                    {
                        putSFTP("5", setFile.putA96_5FilePath(), setFile.putA96_5FileName());
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

        protected void DataToA96(string path1, string path2)
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
                        //arr[0]  ex: 912810QS0@BGN Govt
                        //arr[1]  ex: 03/22/2013
                        //arr[2]  ex: 3.089

                        if (arr.Length >= 3 && !arr[0].IsNullOrWhiteSpace() &&
                            !arr[0].StartsWith("START") && !arr[0].StartsWith("END"))
                        {
                            var ID_CUSIP = arr[0].Trim().Split('@')[0];
                            var dateYLD_YTM_MID = arr[1].Trim();
                            dateYLD_YTM_MID = dateYLD_YTM_MID.Substring(6, 4) + "/" + dateYLD_YTM_MID.Substring(0, 2) + "/" + dateYLD_YTM_MID.Substring(3, 2);
                            var YLD_YTM_MID = arr[2].Trim();

                            YLD_YTM_MID_Class yld = new YLD_YTM_MID_Class();
                            yld.ID_CUSIP = ID_CUSIP;
                            yld.DATE_YLD_YTM_MID = dateYLD_YTM_MID;
                            yld.YLD_YTM_MID = YLD_YTM_MID;
                            listYLD.Add(yld);
                        }
                    }

                    if ("START-OF-DATA".Equals(line))
                    {
                        flag = true;
                    }
                }

                listYLD = listYLD.GroupBy(o => new { o.ID_CUSIP, o.DATE_YLD_YTM_MID, o.YLD_YTM_MID })
                                 .Select(o => o.FirstOrDefault()).ToList();

                foreach (var item in A96Data)
                {
                    var YLDs = listYLD.Where(x => x.ID_CUSIP == item.ID_CUSIP
                                                && (x.DATE_YLD_YTM_MID == item.Report_Date || x.DATE_YLD_YTM_MID == item.Origination_Date)).ToList();

                    foreach (var oneYLD in YLDs)
                    {
                        item.Mid_Yield = oneYLD.YLD_YTM_MID;

                        if (item.Mid_Yield != null && item.Mid_Yield != "")
                        {
                            if (item.Report_Date == oneYLD.DATE_YLD_YTM_MID)
                            {
                                item.Treasury_Current = (double.Parse(item.Mid_Yield) * 100).ToString();
                            }

                            if (item.Origination_Date == oneYLD.DATE_YLD_YTM_MID)
                            {
                                item.Treasury_When_Trade = (double.Parse(item.Mid_Yield) * 100).ToString();
                            }

                            if (item.Treasury_Current != null && item.Treasury_Current != "")
                            {
                                item.Spread_Current = (double.Parse(item.Mid_Yield) * 100 - double.Parse(item.Treasury_Current)).ToString();
                            }

                            if (item.Treasury_When_Trade != null && item.Treasury_When_Trade != "")
                            {
                                item.Spread_When_Trade = (double.Parse(item.EIR) * 100 - double.Parse(item.Treasury_When_Trade)).ToString();
                            }

                            if (item.Spread_Current.IsNullOrWhiteSpace() == false
                                && item.Spread_When_Trade.IsNullOrWhiteSpace() == false
                                && item.Treasury_Current.IsNullOrWhiteSpace() == false
                                && item.Treasury_When_Trade.IsNullOrWhiteSpace() == false
                               )
                            {
                                item.All_in_Chg = (double.Parse(item.Spread_Current)
                                                   - double.Parse(item.Spread_When_Trade)
                                                   + double.Parse(item.Treasury_Current)
                                                   - double.Parse(item.Treasury_When_Trade)).ToString();
                            }

                            if (item.Spread_Current.IsNullOrWhiteSpace() == false
                                && item.Spread_When_Trade.IsNullOrWhiteSpace() == false
                               )
                            {
                                item.Chg_In_Spread = (double.Parse(item.Spread_Current) - double.Parse(item.Spread_When_Trade)).ToString();
                            }

                            if (item.Treasury_Current.IsNullOrWhiteSpace() == false
                                && item.Treasury_When_Trade.IsNullOrWhiteSpace() == false)
                            {
                                item.Chg_In_Treasury = (double.Parse(item.Treasury_Current) - double.Parse(item.Treasury_When_Trade)).ToString();
                            }
                        }
                    }
                }
            }
            #endregion
        }

        protected void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();

            #region saveDb
            try
            {
                //A96Data.RemoveAll(x => x.Reference_Nbr.IsNullOrWhiteSpace() == true
                //                    || x.Report_Date.IsNullOrWhiteSpace() == true
                //                    || x.Version.IsNullOrWhiteSpace() == true
                //                    || x.Bond_Number.IsNullOrWhiteSpace() == true
                //                    || x.Lots.IsNullOrWhiteSpace() == true
                //                    || x.Portfolio_Name.IsNullOrWhiteSpace() == true
                //                    || x.Origination_Date.IsNullOrWhiteSpace() == true
                //                    || x.EIR.IsNullOrWhiteSpace() == true
                //                    || x.Processing_Date.IsNullOrWhiteSpace() == true
                //                    || x.Mid_Yield.IsNullOrWhiteSpace() == true
                //                    || x.Spread_Current.IsNullOrWhiteSpace() == true
                //                    || x.Spread_When_Trade.IsNullOrWhiteSpace() == true
                //                    || x.Treasury_Current.IsNullOrWhiteSpace() == true
                //                    || x.Treasury_When_Trade.IsNullOrWhiteSpace() == true
                //                    || x.All_in_Chg.IsNullOrWhiteSpace() == true
                //                    || x.Chg_In_Spread.IsNullOrWhiteSpace() == true
                //                    || x.Chg_In_Treasury.IsNullOrWhiteSpace() == true
                //                 );

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

                #region 加入 sql transferCheck by Mark 2018/01/09
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
                #region 加入 sql transferCheck by Mark 2018/01/09
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