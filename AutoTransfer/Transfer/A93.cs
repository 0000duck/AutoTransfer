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
    public class A93
    {
        #region 共用參數
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A93;
        private string type = TableType.A93.ToString();
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
               !DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
               System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt))
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
                createA93File();
            }
        }

        protected void createA93File()
        {
            List<Gov_Info_Ticker> A94;

            using (IFRS9DBEntities db = new IFRS9DBEntities())
            {
                A94 = db.Gov_Info_Ticker.AsNoTracking()
                                        .Where(x => x.Foreign_Exchange_Map.ToString() != "")
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

            CreateA93File createFile = new CreateA93File();
            if (createFile.create(reportDateStr, A94))
            {
                //把資料送給SFTP
                putA93SFTP();
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

        protected void putA93SFTP()
        {
            string error = string.Empty;

            error = putToSFTP();
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            getA93SFTP();
        }

        #region putToSFTP
        protected string putToSFTP()
        {
            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                     setFile.putA93FilePath(),
                     setFile.putA93FileName(),
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

        protected void getA93SFTP()
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
            new FileRelated().createFile(setFile.getA93FilePath());

            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Get(string.Empty,
                     setFile.getA93FilePath(),
                     setFile.getA93FileName(),
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

                using (IFRS9DBEntities db = new IFRS9DBEntities())
                {
                    List<Gov_Info_Ticker> listA94 = db.Gov_Info_Ticker.ToList();
                    List<Gov_Info_Monthly> listA93 = db.Gov_Info_Monthly.Where(x=>x.Processing_Date == processingDate).ToList();
                    List<Gov_Info_Monthly> A93Datas = new List<Gov_Info_Monthly>();
                    using (StreamReader sr = new StreamReader(Path.Combine(
                           setFile.getA93FilePath(), setFile.getA93FileName())))
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
                                //arr[0]  ex: "TUIRCBFX Index"
                                //arr[3]  ex: 90196.600000  
                                //arr[4]  ex: 06/30/2017
                                string[] arr = line.Split(',');
                                int okLength = 4;

                                if (arr.Length >= okLength && arr[0].IsNullOrWhiteSpace() == false
                                    && arr[0].StartsWith("START") == false && arr[0].StartsWith("END") == false)
                                {
                                    string index = arr[0].Trim().Replace("\"", "");
                                    string value = arr[3].Trim();

                                    double d = 0d;

                                    if (index.IsNullOrWhiteSpace() == false && double.TryParse(value, out d) == true)
                                    {
                                        string Country = GetCountry(index, listA94);

                                        if (Country != "")
                                        {
                                            var A93DBData = listA93.FirstOrDefault(x => x.Country == Country);
                                            var A93CSVData = A93Datas.FirstOrDefault(x => x.Processing_Date == processingDate
                                                                                       && x.Country == Country);
                                            if (A93DBData != null)
                                            {
                                                A93DBData.Foreign_Exchange = d;
                                                A93DBData.Foreign_Exchange_Map = index;
                                            }
                                            else if (A93CSVData != null)
                                            {
                                                A93CSVData.Foreign_Exchange = d;
                                                A93CSVData.Foreign_Exchange_Map = index;
                                            }
                                            else
                                            {
                                                Gov_Info_Monthly newData = new Gov_Info_Monthly();

                                                newData.Processing_Date = processingDate;
                                                newData.Country = Country;
                                                newData.Foreign_Exchange = d;
                                                newData.Foreign_Exchange_Map = index;

                                                A93Datas.Add(newData);
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

                    db.Gov_Info_Monthly.AddRange(A93Datas);
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

            Gov_Info_Ticker A94 = listA94.Where(x => x.Foreign_Exchange_Map == inputString).FirstOrDefault();
            if (A94 != null)
            {
                country = A94.Country;
            } 

            return country;
        }
    }
}