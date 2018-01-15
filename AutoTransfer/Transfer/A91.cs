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
    public class A91
    {
        #region 共用參數
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A91;
        private string type = TableType.A91.ToString();
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
                createA91File();
            }
        }

        protected void createA91File()
        {
            List<Gov_Info_Ticker> A94;

            using (IFRS9Entities db = new IFRS9Entities())
            {
                A94 = db.Gov_Info_Ticker.AsNoTracking()
                        .Where(x => x.IGS_Index_Map.ToString() != "" || x.GDP_Yearly_Map.ToString() != "")
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

            CreateA91File createFile = new CreateA91File();
            if (createFile.create(reportDateStr, A94))
            {
                //把資料送給SFTP
                putA91SFTP();
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

        protected void putA91SFTP()
        {
            string error = string.Empty;

            error = putToSFTP();
            if (error.IsNullOrWhiteSpace() == false)
            {
                return;
            }

            getA91SFTP();
        }

        #region putToSFTP
        protected string putToSFTP()
        {
            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putA91FilePath(),
                 setFile.putA91FileName(),
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

        protected void getA91SFTP()
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
            new FileRelated().createFile(setFile.getA91FilePath());

            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getA91FilePath(),
                 setFile.getA91GZFileName(),
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
                string sourceFileName = Path.Combine(setFile.getA91FilePath(), setFile.getA91GZFileName());
                string destFileName = Path.Combine(setFile.getA91FilePath(), setFile.getA91FileName());
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
                    List<Gov_Info_Yearly> listA91 = db.Gov_Info_Yearly.ToList();
                    List<Gov_Info_Yearly> A91Datas = new List<Gov_Info_Yearly>();

                    using (StreamReader sr = new StreamReader(Path.Combine(
                        setFile.getA91FilePath(), setFile.getA91FileName())))
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
                                //arr[0]  ex: IGS%TUR Index
                                //arr[1]  ex: 12/31/2016  
                                //arr[2]  ex: 29.1
                                string[] arr = line.Split('|');
                                int okLength = 3;

                                if (arr.Length >= okLength && arr[0].IsNullOrWhiteSpace() == false
                                    && arr[0].StartsWith("START") == false && arr[0].StartsWith("END") == false)
                                {
                                    string index = arr[0].Trim();
                                    string dataDate = arr[1].Trim();
                                    string value = arr[2].Trim();

                                    DateTime dt = DateTime.MinValue;
                                    double d = 0d;

                                    if (index.IsNullOrWhiteSpace() == false
                                        && DateTime.TryParseExact(dataDate, "MM/dd/yyyy", null,
                                           System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dt) == true
                                        && double.TryParse(value, out d) == true)
                                    {
                                        string Data_Year = dataDate.Substring(6,4);
                                        string Country = GetCountry(index, listA94);
                                        string ColumnName = GetColumnName(index, listA94);

                                        if (Country != "" && ColumnName != "")
                                        {
                                            var A91DBData = listA91.FirstOrDefault(x =>x.Data_Year.ToString() == Data_Year && x.Country == Country);
                                            var A91CSVData = A91Datas.FirstOrDefault(x => x.Data_Year.ToString() == Data_Year && x.Country == Country);
                                            if (A91DBData != null)
                                            {
                                                A91DBData.Processing_Date = processingDate;

                                                if (ColumnName == "IGS_Index")
                                                {
                                                    A91DBData.IGS_Index = d;
                                                    A91DBData.IGS_Index_Map = index;
                                                }

                                                if (ColumnName == "GDP_Yearly")
                                                {
                                                    A91DBData.GDP_Yearly = d;
                                                    A91DBData.GDP_Yearly_Map = index;
                                                }
                                            }
                                            else if (A91CSVData != null)
                                            {
                                                A91CSVData.Processing_Date = processingDate;

                                                if (ColumnName == "IGS_Index")
                                                {
                                                    A91CSVData.IGS_Index = d;
                                                    A91CSVData.IGS_Index_Map = index;
                                                }

                                                if (ColumnName == "GDP_Yearly")
                                                {
                                                    A91CSVData.GDP_Yearly = d;
                                                    A91CSVData.GDP_Yearly_Map = index;
                                                }
                                            }
                                            else
                                            {
                                                Gov_Info_Yearly newData = new Gov_Info_Yearly();

                                                newData.Data_Year = int.Parse(Data_Year);
                                                newData.Processing_Date = processingDate;
                                                newData.Country = Country;

                                                if (ColumnName == "IGS_Index")
                                                {
                                                    newData.IGS_Index = d;
                                                    newData.IGS_Index_Map = index;
                                                }

                                                if (ColumnName == "GDP_Yearly")
                                                {
                                                    newData.GDP_Yearly = d;
                                                    newData.GDP_Yearly_Map = index;
                                                }

                                                A91Datas.Add(newData);
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

                    db.Gov_Info_Yearly.AddRange(A91Datas);
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

            Gov_Info_Ticker A94 = listA94.Where(x => x.IGS_Index_Map == inputString || x.GDP_Yearly_Map == inputString)
                                         .FirstOrDefault();
            if (A94 != null)
            {
                country = A94.Country;
            }

            return country;
        }

        private string GetColumnName(string inputString, List<Gov_Info_Ticker> listA94)
        {
            string columnName = "";

            Gov_Info_Ticker A94 = listA94.Where(x => x.IGS_Index_Map == inputString)
                                         .FirstOrDefault();
            if (A94 != null)
            {
                columnName = "IGS_Index";
            }

            A94 = listA94.Where(x => x.GDP_Yearly_Map == inputString)
                         .FirstOrDefault();
            if (A94 != null)
            {
                columnName = "GDP_Yearly";
            }

            return columnName;
        }
    }
}