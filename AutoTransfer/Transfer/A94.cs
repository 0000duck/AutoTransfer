﻿using AutoTransfer.CreateFile;
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
    public class A94
    {
        #region 共用參數
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A94;
        private string type = TableType.A94.ToString();
        private string yqm = "";
        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public void startTransfer(string dateTime, string YearQuarterMonth)
        {
            yqm = YearQuarterMonth;

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
                createA94File();
            }
        }

        /// <summary>
        /// 建立 A94 要Put的檔案
        /// </summary>
        protected void createA94File()
        {
            CreateA94File createFile = new CreateA94File();

            if (createFile.create(reportDateStr,"y") && createFile.create(reportDateStr, "q") && createFile.create(reportDateStr, "m"))
            {
                //把資料送給SFTP
                putA94SFTP();
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

        /// <summary>
        /// SFTP Put A94檔案
        /// </summary>
        protected void putA94SFTP()
        {
            string error = string.Empty;

            error = putToSFTP("y");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = putToSFTP("q");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = putToSFTP("m");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            getA94SFTP();
        }

        #region putToSFTP
        protected string putToSFTP(string yqm)
        {
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Put(string.Empty,
            //     setFile.putA94FilePath(),
            //     setFile.putA94FileName(yqm),
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

        /// <summary>
        /// SFTP Get A94檔案
        /// </summary>
        protected void getA94SFTP()
        {
            string error = string.Empty;

            error = getFromSFTP("y");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = getFromSFTP("q");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = getFromSFTP("m");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = DataToDb("y");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = DataToDb("q");
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            error = DataToDb("m");
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
        protected string getFromSFTP(string yqm)
        {
            new FileRelated().createFile(setFile.getA94FilePath());

            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //.Get(string.Empty,
            //     setFile.getA94FilePath(),
            //     setFile.getA94GZFileName(yqm),
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
        protected string DataToDb(string yqm)
        {
            string error = string.Empty;

            try
            {
                IFRS9Entities db = new IFRS9Entities();

                List<Gov_Info_Ticker> listA94 = db.Gov_Info_Ticker.ToList();
                List<Gov_Info_Ticker> A94Datas = new List<Gov_Info_Ticker>();
                Gov_Info_Ticker A94 = null;
                using (StreamReader sr = new StreamReader(Path.Combine(
                    setFile.getA94FilePath(), setFile.getA94FileName(yqm))))
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
                            string[] arr = line.Split('|');
                            int okLength = 0;

                            if (yqm == "y" || yqm == "q")
                            {
                                //arr[0]  ex: IGS%TUR Index
                                //arr[1]  ex: 12/30/2016
                                //arr[2]  ex: 28.13

                                arr = line.Split('|');
                                okLength = 3;
                            }
                            else if (yqm == "m")
                            {
                                //arr[0]  ex: "TUIRCBFX Index"
                                //arr[3]  ex: 90196.600000  
                                //arr[4]  ex: 06/30/2017

                                arr = line.Split(',');
                                okLength = 4;
                            }
                            
                            if (arr.Length >= okLength && arr[0].IsNullOrWhiteSpace() == false 
                                && arr[0].StartsWith("START") == false && arr[0].StartsWith("END") == false)
                            {
                                string index = arr[0].Trim().Replace("\"","");
                                string value = "";

                                if (yqm == "y" || yqm == "q")
                                {
                                    value = arr[2].Trim();
                                }
                                else if (yqm == "m")
                                {
                                    value = arr[3].Trim();
                                }

                                if (index.IsNullOrWhiteSpace() == false)
                                {
                                    var Country = GetCountry(index);
                                    var ColumnName = GetColumnName(index);

                                    if (Country != "" && ColumnName != "")
                                    {
                                        A94 = listA94.FirstOrDefault(x => x.Country == Country);
                                        var A94Data = A94Datas.FirstOrDefault(x => x.Country == Country);

                                        if (A94 != null)
                                        {
                                            var A94pro = A94.GetType().GetProperties()
                                                         .Where(x => x.Name == ColumnName)
                                                         .FirstOrDefault();
                                            if (A94pro != null)
                                            {
                                                A94pro.SetValue(A94, value);
                                            }
                                        }
                                        else if (A94Data != null)
                                        {
                                            var A94pro = A94Data.GetType().GetProperties()
                                                         .Where(x => x.Name == ColumnName)
                                                         .FirstOrDefault();
                                            if (A94pro != null)
                                            {
                                                A94pro.SetValue(A94Data, value);
                                            }
                                        }
                                        else
                                        {
                                            Gov_Info_Ticker newData = new Gov_Info_Ticker();
                                            newData.Country = Country;
                                            var A94pro = newData.GetType().GetProperties()
                                                         .Where(x => x.Name == ColumnName)
                                                         .FirstOrDefault();
                                            if (A94pro != null)
                                            {
                                                A94pro.SetValue(newData, value);
                                            }

                                            A94Datas.Add(newData);
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

                db.Gov_Info_Ticker.AddRange(A94Datas);
                db.SaveChanges();
                db.Dispose();
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

        private string GetCountry (string inputString)
        {
            string country = "";

            switch (inputString)
            {
                case "IGS%TUR Index":
                case "HELDTRDS Index":
                case "TUIRCBFX Index":
                case "EHGDTRY Index":
                    country = "土耳其";
                    break;
                case "IGS%BRA Index":
                case "HELDBRDS Index":
                case "BZIDFCUR Index":
                case "EHGDBRY Index":
                    country = "巴西";
                    break;
                case "IGS%ISR Index":
                case "HELDILDS Index":
                case "ISFCBAL Index":
                case "EHGDILY Index":
                    country = "以色列";
                    break;
                case "IGS%QAT Index":
                case "453.055 Index":
                case "EHGDQAY Index":
                    country = "卡達";
                    break;
                case "IGS%IND Index":
                case "HELDIDDS Index":
                case "IDGFA Index":
                case "EHGDIDY Index":
                    country = "印尼";
                    break;
                case "IGS%SAU Index":
                case "456.055 Index":
                case "SRGDPCYY Index":
                    country = "沙烏地阿拉伯";
                    break;
                case "IGS%RUS Index":
                case "HELDRUDS Index":
                case "RUFGGFML Index":
                case "EHGDRUY Index":
                    country = "俄羅斯";
                    break;
                case "IGS%ZAF Index":
                case "HELDZA59 Index":
                case "SANOGR$ Index":
                case "EHGDZAY Index":
                    country = "南非";
                    break;
                case "IGS%MEX Index":
                case "HELDMXS Index":
                case "MXIRINUS Index":
                case "EHGDMXY Index":
                    country = "墨西哥";
                    break;
                case "IGS%CHL Index":
                case "HELDCLS Index":
                case "CHMRRSRV Index":
                case "EHGDCLY Index":
                    country = "智利";
                    break;
                case "IGS%PHL Index":
                case "HELDPHS Index":
                case "PHIRTTL Index":
                case "EHGDPHY Index":
                    country = "菲律賓";
                    break;
                case "IGS%USA Index":
                case "HELDUSDS Index":
                case "WIRAUS Index":
                case "EHGDUSY Index":
                    country = "美國";
                    break;
            }

            return country;
        }

        private string GetColumnName(string inputString)
        {
            string columnName = "";

            switch (inputString)
            {
                case "IGS%TUR Index":
                case "IGS%BRA Index":
                case "IGS%ISR Index":
                case "IGS%QAT Index":
                case "IGS%IND Index":
                case "IGS%SAU Index":
                case "IGS%RUS Index":
                case "IGS%ZAF Index":
                case "IGS%MEX Index":
                case "IGS%CHL Index":
                case "IGS%PHL Index":
                case "IGS%USA Index":
                    columnName = "IGS_Index_Map";
                    break;
                case "HELDTRDS Index":
                case "HELDBRDS Index":
                case "HELDILDS Index":
                case "HELDIDDS Index":
                case "HELDRUDS Index":
                case "HELDZA59 Index":
                case "HELDMXS Index":
                case "HELDCLS Index":
                case "HELDPHS Index":
                case "HELDUSDS Index":
                    columnName = "Short_term_Debt_Map";
                    break;
                case "TUIRCBFX Index":
                case "BZIDFCUR Index":
                case "ISFCBAL Index":
                case "453.055 Index":
                case "IDGFA Index":
                case "456.055 Index":
                case "RUFGGFML Index":
                case "SANOGR$ Index":
                case "MXIRINUS Index":
                case "CHMRRSRV Index":
                case "PHIRTTL Index":
                case "WIRAUS Index":
                    columnName = "Foreign_Exchange_Map";
                    break;
                case "EHGDTRY Index":
                case "EHGDBRY Index":
                case "EHGDILY Index":
                case "EHGDQAY Index":
                case "EHGDIDY Index":
                case "SRGDPCYY Index":
                case "EHGDRUY Index":
                case "EHGDZAY Index":
                case "EHGDMXY Index":
                case "EHGDCLY Index":
                case "EHGDPHY Index":
                case "EHGDUSY Index":
                    columnName = "GDP_Yearly_Map";
                    break;
            }

            return columnName;
        }
    }
}