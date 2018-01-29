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
    public class A07
    {
        #region 共用參數
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A07;
        private string type = TableType.A07.ToString();
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
                #region 加入 sql transferCheck by Mark 2018/01/09
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
                #endregion
            }
            else
            {
                reportDateStr = dateTime;
                setFile = new SetFile(tableType, dateTime);
                createA07File();
            }
        }

        /// <summary>
        /// 建立 A07 要Put的檔案
        /// </summary>
        protected void createA07File()
        {
            //建立  檔案
            // create ex:GetA07
            if (new CreateA07File().create(tableType, reportDateStr))
            {
                //把資料送給SFTP
                putA07SFTP();
            }
            else
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
                    MessageType.Create_File_Fail.GetDescription(type)
                    );
                #endregion
            }
        }

        /// <summary>
        /// SFTP Put A07檔案
        /// </summary>
        protected void putA07SFTP()
        {
            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putA07FilePath(),
                 setFile.putFileName(),
                 out error);

            if (!error.IsNullOrWhiteSpace()) //fail
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
                    error
                    );
                #endregion
            }
            else //success (wait 20 min and get data)
            {
                Thread.Sleep(20 * 60 * 1000);
                getA07SFTP();
            }
        }

        /// <summary>
        /// SFTP Get A07檔案
        /// </summary>
        protected void getA07SFTP()
        {
            new FileRelated().createFile(setFile.getA07FilePath());

            string error = string.Empty;

            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getA07FilePath(),
                 setFile.getGZFileName(),
                 out error);

            if (!error.IsNullOrWhiteSpace())
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
                    error
                    );
                #endregion
            }
            else
            {
                string sourceFileName = Path.Combine(
                setFile.getA07FilePath(), setFile.getGZFileName());
                string destFileName = Path.Combine(
                setFile.getA07FilePath(), setFile.getFileName());
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
            List<Econ_Domestic> A07Datas = new List<Econ_Domestic>();
            string date = startTime.ToString("yyyyMMdd");
            
            #region A07 Data
            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getA07FilePath(), setFile.getFileName())))
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
                        //arr[0]  ex: TWSE Index
                        //arr[1]  ex: 03/31/2016
                        //arr[2]  ex: 8744.83

                        if (arr.Length >= 3 && !arr[0].IsNullOrWhiteSpace() &&
                            !arr[0].StartsWith("START") && !arr[0].StartsWith("END"))
                        {
                            Econ_Domestic ef = new Econ_Domestic();

                            DateTime dt = DateTime.MinValue;
                            double d = 0d;
                            string index = arr[0].Trim();

                            if (arr[2] != null && double.TryParse(arr[2], out d) &&
                                DateTime.TryParseExact(arr[1], "MM/dd/yyyy", null,
                                System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                                out dt) && !index.IsNullOrWhiteSpace())
                            {
                                var YQ = dt.Year.ToString() + dt.Month.IntToYearQuartly();
                                var A07 = db.Econ_Domestic.Where(x => x.Year_Quartly == YQ).FirstOrDefault();
                                var A07Data = A07Datas.FirstOrDefault(x => x.Year_Quartly == YQ);
                                if (A07 != null)
                                {
                                    var A07pro = A07.GetType().GetProperties()
                                         .Where(x => x.Name.Replace("_", " ") == index.Replace("_", " ")).FirstOrDefault();
                                    if (A07pro != null)
                                    {
                                        A07pro.SetValue(A07, d);
                                        A07.Date = date;
                                    }
                                }
                                else if (A07Data != null)
                                {
                                    var A07pro = A07Data.GetType().GetProperties()
                                         .Where(x => x.Name.Replace("_", " ") == index.Replace("_", " ")).FirstOrDefault();
                                    if (A07pro != null)
                                    {
                                        A07pro.SetValue(A07Data, d);
                                        A07Data.Date = date;
                                    }
                                }
                                else
                                {
                                    Econ_Domestic newData = new Econ_Domestic();
                                    newData.Year_Quartly = YQ;
                                    var A07pro = newData.GetType().GetProperties()
                                        .Where(x => x.Name.Replace("_", " ") == index.Replace("_", " ")).FirstOrDefault();
                                    if (A07pro != null)
                                    {
                                        A07pro.SetValue(newData, d);
                                        newData.Date = date;
                                    }

                                    A07Datas.Add(newData);
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
            #endregion A07 Data

            #region saveDb
            try
            {
                db.Econ_Domestic.AddRange(A07Datas);
                db.SaveChanges();
                db.Dispose();
                #region 加入 sql transferCheck by Mark 2018/01/09
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
                #endregion
            }
            catch (DbUpdateException ex)
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