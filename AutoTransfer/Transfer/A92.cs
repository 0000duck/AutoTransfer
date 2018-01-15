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
    public class A92
    {
        #region 共用參數
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A92;
        private string type = TableType.A92.ToString();
        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public void startTransfer(string dateTime)
        {
            logPath = log.txtLocation(type);

            startTime = DateTime.Now;

            if (dateTime.Length != 8 ||
                DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt) == false)
            {
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
            else
            {
                reportDateStr = dateTime;
                setFile = new SetFile(tableType, dateTime);
                createA92File();
            }
        }

        protected void createA92File()
        {
            List<Gov_Info_Ticker> A94;

            using (IFRS9Entities db = new IFRS9Entities())
            {
                A94 = db.Gov_Info_Ticker.AsNoTracking()
                                        .Where(x => x.Short_term_Debt_Map.ToString() != "")
                                        .ToList();
                if (A94.Any() == false)
                {
                    log.bothLog(
                        type,
                        false,
                        reportDateDt,
                        startTime,
                        DateTime.Now,
                        1,
                        logPath,
                        "A94-主權債測試指標_Ticker 沒有做相關設定"
                     );

                    return;
                }
            }

            CreateA92File createFile = new CreateA92File();
            if (createFile.create(reportDateStr, A94))
            {
                //把資料送給SFTP
                putA92SFTP();
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
                    MessageType.Create_File_Fail.GetDescription(type)
                );
            }
        }

        protected void putA92SFTP()
        {
            string error = string.Empty;

            error = putToSFTP();
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            getA92SFTP();
        }

        #region putToSFTP
        protected string putToSFTP()
        {
            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                     setFile.putA92FilePath(),
                     setFile.putA92FileName(),
                     out error);

            if (error.IsNullOrWhiteSpace() == false)
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    error
                 );
            }
            else
            {
                Thread.Sleep(20 * 60 * 1000);
            }

            return error;
        }
        #endregion

        protected void getA92SFTP()
        {
            string error = string.Empty;

            error = getFromSFTP();
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = DataToDb();
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            log.bothLog(
                type,
                true,
                reportDateDt,
                startTime,
                DateTime.Now,
                1,
                logPath,
                MessageType.Success.GetDescription()
            );
        }

        #region getFromSFTP
        protected string getFromSFTP()
        {
            new FileRelated().createFile(setFile.getA92FilePath());

            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Get(string.Empty,
                     setFile.getA92FilePath(),
                     setFile.getA92GZFileName(),
                     out error);

            if (error.IsNullOrWhiteSpace() == false)
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    error
                 );
            }
            else
            {
                string sourceFileName = Path.Combine(setFile.getA92FilePath(), setFile.getA92GZFileName());
                string destFileName = Path.Combine(setFile.getA92FilePath(), setFile.getA92FileName());
                Extension.Decompress(sourceFileName, destFileName);
            }

            return error;
        }
        #endregion

        /// <summary>
        /// Db save
        /// </summary>
        protected string DataToDb()
        {
            string error = string.Empty;

            try
            {
                DateTime processingDate = DateTime.Parse(startTime.ToString("yyyy/MM/dd"));

                using (IFRS9Entities db = new IFRS9Entities())
                {
                    List<Gov_Info_Ticker> listA94 = db.Gov_Info_Ticker.ToList();
                    List<Gov_Info_Quartly> listA92 = db.Gov_Info_Quartly.Where(x => x.Processing_Date == processingDate).ToList();
                    List<Gov_Info_Quartly> A92Datas = new List<Gov_Info_Quartly>();
                    using (StreamReader sr = new StreamReader(Path.Combine(
                           setFile.getA92FilePath(), setFile.getA92FileName())))
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
                                //arr[0]  ex: HELDTRDS Index
                                //arr[1]  ex: 03/31/2016  
                                //arr[2]  ex: 105812.729
                                string[] arr = line.Split('|');
                                int okLength = 3;

                                if (arr.Length >= okLength && arr[0].IsNullOrWhiteSpace() == false
                                    && arr[0].StartsWith("START") == false && arr[0].StartsWith("END") == false)
                                {
                                    string index = arr[0].Trim();
                                    string value = arr[2].Trim();

                                    double d = 0d;

                                    if (index.IsNullOrWhiteSpace() == false && double.TryParse(value, out d) == true)
                                    {
                                        string Country = GetCountry(index, listA94);

                                        if (Country != "")
                                        {
                                            var A92DBData = listA92.FirstOrDefault(x => x.Country == Country);
                                            var A92CSVData = A92Datas.FirstOrDefault(x => x.Processing_Date == processingDate
                                                                                       && x.Country == Country);
                                            if (A92DBData != null)
                                            {
                                                A92DBData.Short_term_Debt = d;
                                                A92DBData.Short_term_Debt_Map = index;
                                            }
                                            else if (A92CSVData != null)
                                            {
                                                A92CSVData.Short_term_Debt = d;
                                                A92CSVData.Short_term_Debt_Map = index;
                                            }
                                            else
                                            {
                                                Gov_Info_Quartly newData = new Gov_Info_Quartly();

                                                newData.Processing_Date = processingDate;
                                                newData.Country = Country;
                                                newData.Short_term_Debt = d;
                                                newData.Short_term_Debt_Map = index;

                                                A92Datas.Add(newData);
                                            }
                                        }
                                    }
                                }
                            }

                            if ("START-OF-DATA".Equals(line))
                            {
                                flag = true;
                            }
                        }
                    }

                    db.Gov_Info_Quartly.AddRange(A92Datas);
                    db.SaveChanges();
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

                error = ex.Message;
            }

            return error;
        }

        private string GetCountry(string inputString, List<Gov_Info_Ticker> listA94)
        {
            string country = "";

            Gov_Info_Ticker A94 = listA94.Where(x => x.Short_term_Debt_Map == inputString).FirstOrDefault();
            if (A94 != null)
            {
                country = A94.Country;
            }

            return country;
        }
    }
}