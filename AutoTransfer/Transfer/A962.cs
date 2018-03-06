using AutoTransfer.BondSpreadInfo;
using AutoTransfer.CreateFile;
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
    public class A962
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
        private TableType tableType = TableType.A962;
        private string type = TableType.A962.ToString();
        public List<Bond_Spread_Info> A96Data = new List<Bond_Spread_Info>();
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

            A96Data = db.Bond_Spread_Info.AsNoTracking().Where(x=>x.Report_Date == reportDateDt).ToList();
            verInt = db.Bond_Spread_Info.AsNoTracking()
                       .Where(x => x.Report_Date == reportDateDt)
                       .DefaultIfEmpty().Max(x => x.Version);

            if (A96Data.Any() == false)
            {
                db.Dispose();

                List<string> errs = new List<string>();

                errs.Add("Bond_Spread_Info 無資料，請先執行 A961");

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
                db.Dispose();

                reportDateStr = dateTime;
                setFile = new SetFile(tableType, dateTime);

                createA9621File();
            }
        }

        protected void createA9621File()
        {
            if (new CreateA9621File().create(tableType, reportDateStr, A96Data))
            {
                putSFTP("1", setFile.putA9621FilePath(), setFile.putA9621FileName());
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
                        getFilePath = setFile.getA9621FilePath();
                        getFileName = setFile.getA9621GZFileName();
                        break;
                    case "2":
                        getFilePath = setFile.getA9622FilePath();
                        getFileName = setFile.getA9622GZFileName();
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
                        destFileName = Path.Combine(getFilePath, setFile.getA9621FileName());
                        Extension.Decompress(sourceFileName, destFileName);
                        DataTreasuryToA96(setFile.getA9621FilePath(), setFile.getA9621FileName(), "Current");
                        createA9622File();
                        break;
                    case "2":
                        sourceFileName = Path.Combine(getFilePath, getFileName);
                        destFileName = Path.Combine(getFilePath, setFile.getA9622FileName());
                        Extension.Decompress(sourceFileName, destFileName);
                        DataTreasuryToA96(setFile.getA9622FilePath(), setFile.getA9622FileName(), "When_Trade");
                        DataToDb();
                        break;
                    default:
                        break;
                }
            }
        }

        protected void DataTreasuryToA96(string path1, string path2, string TreasuryType)
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
                    string reportDate = item.Report_Date.ToString("yyyy/MM/dd");
                    string originationDate = item.Origination_Date.ToString("yyyy/MM/dd");


                    var YLDs = listYLD.Where(x => x.ID_CUSIP == item.ID_CUSIP
                                              && (x.DATE_YLD_YTM_MID == reportDate)).ToList();

                    if (TreasuryType == "When_Trade")
                    {
                        YLDs = listYLD.Where(x => x.ID_CUSIP == item.ID_CUSIP
                                              && (x.DATE_YLD_YTM_MID == originationDate)).ToList();
                    }

                    foreach (var oneYLD in YLDs)
                    {
                        if (oneYLD.YLD_YTM_MID != null && oneYLD.YLD_YTM_MID != "")
                        {
                            if (TreasuryType == "Current")
                            {
                                item.Treasury_Current = double.Parse(oneYLD.YLD_YTM_MID) * 100;
                            }
                            else if (TreasuryType == "When_Trade")
                            {
                                item.Treasury_When_Trade = double.Parse(oneYLD.YLD_YTM_MID) * 100;
                            }
                        }
                    }
                }
            }
            #endregion
        }

        protected void createA9622File()
        {
            string minOriginationDate = A96Data.DefaultIfEmpty().Min(x => x.Origination_Date).ToString("yyyyMMdd");
            string maxOriginationDate = A96Data.DefaultIfEmpty().Max(x => x.Origination_Date).ToString("yyyyMMdd");

            if (new CreateA9622File().create(tableType, minOriginationDate, maxOriginationDate, reportDateStr, A96Data))
            {
                putSFTP("2", setFile.putA9622FilePath(), setFile.putA9622FileName());
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

        protected void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();

            #region saveDb
            try
            {
                StringBuilder sb = new StringBuilder();

                foreach (var item in A96Data)
                {
                    if (item.Mid_Yield != null && item.Treasury_Current != null)
                    {
                        item.Spread_Current = item.Mid_Yield * 100 - item.Treasury_Current;
                    }

                    if (item.Treasury_When_Trade != null)
                    {
                        item.Spread_When_Trade = item.EIR * 100 - item.Treasury_When_Trade;
                    }

                    if (item.Spread_Current != null
                        && item.Spread_When_Trade != null
                        && item.Treasury_Current != null
                        && item.Treasury_When_Trade != null
                       )
                    {
                        item.All_in_Chg = item.Spread_Current
                                           - item.Spread_When_Trade
                                           + item.Treasury_Current
                                           - item.Treasury_When_Trade;
                    }

                    if (item.Spread_Current != null && item.Spread_When_Trade != null)
                    {
                        item.Chg_In_Spread = item.Spread_Current - item.Spread_When_Trade;
                    }

                    if (item.Treasury_Current != null && item.Treasury_When_Trade != null)
                    {
                        item.Chg_In_Treasury = item.Treasury_Current - item.Treasury_When_Trade;
                    }

                    var Spread_Current = (item.Spread_Current == null ? "NULL": item.Spread_Current.ToString());
                    var Spread_When_Trade = (item.Spread_When_Trade == null ? "NULL" : item.Spread_When_Trade.ToString());
                    var Treasury_Current = (item.Treasury_Current == null ? "NULL" : item.Treasury_Current.ToString());
                    var Treasury_When_Trade = (item.Treasury_When_Trade == null ? "NULL" : item.Treasury_When_Trade.ToString());
                    var All_in_Chg = (item.All_in_Chg == null ? "NULL" : item.All_in_Chg.ToString());
                    var Chg_In_Spread = (item.Chg_In_Spread == null ? "NULL" : item.Chg_In_Spread.ToString());
                    var Chg_In_Treasury = (item.Chg_In_Treasury == null ? "NULL" : item.Chg_In_Treasury.ToString());

                    sb.Append($@"
                                    UPDATE Bond_Spread_Info SET Spread_Current = {Spread_Current},
                                                                Spread_When_Trade = {Spread_When_Trade},
                                                                Treasury_Current = {Treasury_Current},
                                                                Treasury_When_Trade = {Treasury_When_Trade},
                                                                All_in_Chg = {All_in_Chg},
                                                                Chg_In_Spread = {Chg_In_Spread},
                                                                Chg_In_Treasury = {Chg_In_Treasury}
                                    WHERE Reference_Nbr = '{item.Reference_Nbr}';
                               ");
                }

                db.Database.ExecuteSqlCommand(sb.ToString());
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