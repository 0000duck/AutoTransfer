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
    public class A84
    {
        #region 共用參數

        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A84;
        private string type = TableType.A84.ToString();

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
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.DateTime_Format_Fail.GetDescription()
                    );
            }
            else
            {
                reportDateStr = dateTime;
                //A84 的傳送檔名為 GetC04.req
                setFile = new SetFile(TableType.C04, dateTime);
                createA84File();
            }
        }

        /// <summary>
        /// 建立 A84 要Put的檔案
        /// </summary>
        protected void createA84File()
        {
            //建立  檔案
            // create ex:GetC04
            if (new CreateGetC04File().create(reportDateStr))
            {
                //把資料送給SFTP
                putA84SFTP();
            }
            else
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Create_File_Fail.GetDescription(type));
            }
        }

        /// <summary>
        /// SFTP Put A84檔案
        /// </summary>
        protected void putA84SFTP()
        {
            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putC04FilePath(),
                 setFile.putFileName(),
                 out error);
            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    error);
            }
            else //success (wait 20 min and get data)
            {
                Thread.Sleep(20 * 60 * 1000);
                getA84SFTP();
            }
        }

        /// <summary>
        /// SFTP Get A07檔案
        /// </summary>
        protected void getA84SFTP()
        {
            new FileRelated().createFile(setFile.getC04FilePath());

            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //.Get(string.Empty,
            //     setFile.getC04FilePath(),
            //     setFile.getFileName(),
            //     out error);
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getC04FilePath(),
                 setFile.getGZFileName(),
                 out error);
            if (!error.IsNullOrWhiteSpace())
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    error);
            }
            else
            {
                string sourceFileName = Path.Combine(
                setFile.getC04FilePath(), setFile.getGZFileName());
                string destFileName = Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName());
                Extension.Decompress(sourceFileName, destFileName);
                DataToDb();
            }
        }

        /// <summary>
        /// Db save
        /// </summary>
        protected void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();
            List<Econ_Foreign> A84Datas = new List<Econ_Foreign>();
            string date = startTime.ToString("yyyyMMdd");
            #region A84 Data

            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName())))
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
                        //arr[0]  ex: CPI INDX Index 
                        //arr[1]  ex: 03/31/2016
                        //arr[2]  ex: 8744.83

                        if (arr.Length >= 3 && !arr[0].IsNullOrWhiteSpace() &&
                            !arr[0].StartsWith("START") && !arr[0].StartsWith("END"))
                        {
                            Econ_Foreign ef = new Econ_Foreign();

                            DateTime dt = DateTime.MinValue;
                            double d = 0d;
                            string index = arr[0].Trim();
                            if (arr[2] != null && double.TryParse(arr[2],out d) &&
                                DateTime.TryParseExact(arr[1], "MM/dd/yyyy", null,
                                System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                                out dt) && !index.IsNullOrWhiteSpace())
                            {
                                if (index == "CNFRBAL$ Index") //貿易收支 要排除$
                                    index = "CNFRBAL Index"; 
                                var YQ = dt.Year.ToString() + dt.Month.IntToYearQuartly();
                                var A84 = db.Econ_Foreign.Where(x => x.Year_Quartly == YQ).FirstOrDefault();
                                var A84Data = A84Datas.FirstOrDefault(x => x.Year_Quartly == YQ);
                                if (A84 != null)
                                {
                                    var A84pro = A84.GetType().GetProperties()
                                         .Where(x => x.Name.Replace("_", " ") == index).FirstOrDefault();
                                    if (A84pro != null)
                                    {
                                        A84pro.SetValue(A84, d);
                                        A84.Date = date;
                                    }                                        
                                }
                                else if (A84Data != null)
                                {
                                    var A84pro = A84Data.GetType().GetProperties()
                                         .Where(x => x.Name.Replace("_", " ") == index).FirstOrDefault();
                                    if (A84pro != null)
                                    {
                                        A84pro.SetValue(A84Data, d);
                                        A84Data.Date = date;
                                    }
                                        
                                }
                                else
                                {
                                    Econ_Foreign newData = new Econ_Foreign();
                                    newData.Year_Quartly = YQ;
                                    var A84pro = newData.GetType().GetProperties()
                                        .Where(x => x.Name.Replace("_", " ") == index).FirstOrDefault();
                                    if (A84pro != null)
                                    {
                                        A84pro.SetValue(newData, d);
                                        newData.Date = date;
                                    }                                      
                                    A84Datas.Add(newData);
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

            #endregion A84 Data

            #region saveDb

            try
            {
                db.Econ_Foreign.AddRange(A84Datas);
                db.SaveChanges();
                db.Dispose();
                log.txtLog(
                    type,
                    true,
                    startTime,
                    logPath,
                    MessageType.Success.GetDescription());
            }
            catch (DbUpdateException ex)
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    $"message: {ex.Message}" +
                    $", inner message {ex.InnerException?.InnerException?.Message}");
            }

            #endregion saveDb
        }
    }
}