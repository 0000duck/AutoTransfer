using AutoTransfer.CreateFile;
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
            CreateA93File createFile = new CreateA93File();

            if (createFile.create(reportDateStr))
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

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Put(string.Empty,
            //     setFile.putA93FilePath(),
            //     setFile.putA93FileName(),
            //     out error);

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
                //Thread.Sleep(20 * 60 * 1000);
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

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //.Get(string.Empty,
            //     setFile.getA93FilePath(),
            //     setFile.getA93FileName(),
            //     out error);

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
                //string date = startTime.ToString("yyyyMMdd");

                //IFRS9Entities db = new IFRS9Entities();

                //List<Gov_Info_Ticker> listA94 = db.Gov_Info_Ticker.ToList();
                //List<Gov_Info_Monthly> listA93 = db.Gov_Info_Monthly.ToList();
                //using (StreamReader sr = new StreamReader(Path.Combine(
                //    setFile.getA93FilePath(), setFile.getA93FileName())))
                //{
                //    bool flag = false; //判斷是否為要讀取的資料行數
                //    string line = string.Empty;
                //    while ((line = sr.ReadLine()) != null)
                //    {
                //        if ("END-OF-DATA".Equals(line))
                //        {
                //            flag = false;
                //        }

                //        if (flag) //找到的資料
                //        {
                //            string[] arr = line.Split('|');
                //            int okLength = 0;

                //            if (yqm == "y" || yqm == "q")
                //            {
                //                //arr[0]  ex: IGS%TUR Index
                //                //arr[1]  ex: 12/30/2016
                //                //arr[2]  ex: 28.13

                //                arr = line.Split('|');
                //                okLength = 3;
                //            }
                //            else if (yqm == "m")
                //            {
                //                //arr[0]  ex: "TUIRCBFX Index"
                //                //arr[3]  ex: 90196.600000  
                //                //arr[4]  ex: 06/30/2017

                //                arr = line.Split(',');
                //                okLength = 4;
                //            }

                //            if (arr.Length >= okLength && arr[0].IsNullOrWhiteSpace() == false
                //                && arr[0].StartsWith("START") == false && arr[0].StartsWith("END") == false)
                //            {
                //                string index = arr[0].Trim().Replace("\"", "");
                //                string value = "";

                //                if (yqm == "y" || yqm == "q")
                //                {
                //                    value = arr[2].Trim();
                //                }
                //                else if (yqm == "m")
                //                {
                //                    value = arr[3].Trim();
                //                }

                //                if (index.IsNullOrWhiteSpace() == false)
                //                {
                //                    var Country = GetCountry(index);
                //                    var ColumnName = GetColumnName(index);

                //                    if (Country != "" && ColumnName != "")
                //                    {
                //                        A94 = listA94.FirstOrDefault(x => x.Country == Country);
                //                        var A94Data = A94Datas.FirstOrDefault(x => x.Country == Country);

                //                        if (A94 != null)
                //                        {
                //                            var A94pro = A94.GetType().GetProperties()
                //                                         .Where(x => x.Name == ColumnName)
                //                                         .FirstOrDefault();
                //                            if (A94pro != null)
                //                            {
                //                                A94pro.SetValue(A94, value);
                //                            }
                //                        }
                //                        else if (A94Data != null)
                //                        {
                //                            var A94pro = A94Data.GetType().GetProperties()
                //                                         .Where(x => x.Name == ColumnName)
                //                                         .FirstOrDefault();
                //                            if (A94pro != null)
                //                            {
                //                                A94pro.SetValue(A94Data, value);
                //                            }
                //                        }
                //                        else
                //                        {
                //                            Gov_Info_Ticker newData = new Gov_Info_Ticker();
                //                            newData.Country = Country;
                //                            var A94pro = newData.GetType().GetProperties()
                //                                         .Where(x => x.Name == ColumnName)
                //                                         .FirstOrDefault();
                //                            if (A94pro != null)
                //                            {
                //                                A94pro.SetValue(newData, value);
                //                            }

                //                            A94Datas.Add(newData);
                //                        }
                //                    }
                //                }
                //            }
                //        }

                //        if ("START-OF-DATA".Equals(line))
                //        {
                //            flag = true;
                //        }
                //    }
                //}

                //db.Gov_Info_Ticker.AddRange(A94Datas);
                //db.SaveChanges();
                //db.Dispose();
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
    }
}