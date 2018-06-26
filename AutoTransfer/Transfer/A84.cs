using AutoTransfer.CreateFile;
using AutoTransfer.SFTPConnect;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private string type = TableType.A84.ToString();
        private string _SystemUser = "System";
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
                //A84 的傳送檔名為 GetC04.req
                setFile = new SetFile(TableType.C04, dateTime);
                createA84File();
                //getA84SFTP();
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
        /// SFTP Put A84檔案
        /// </summary>
        protected void putA84SFTP()
        {
            string error = string.Empty;
            string error2 = string.Empty;
            string error3 = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putC04FilePath(),
                 setFile.putFileName("1"),
                 out error);
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putC04FilePath(),
                 setFile.putFileName("2"),
                 out error2);
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putC04FilePath(),
                 setFile.putFileName("3"),
                 out error3);
            if (!error.IsNullOrWhiteSpace() ||
                !error2.IsNullOrWhiteSpace() ||
                !error3.IsNullOrWhiteSpace()) //fail
            {
                string _error =
                    !error.IsNullOrWhiteSpace() ? error :
                    !error2.IsNullOrWhiteSpace() ? error2 : error3;
                #region 加入 sql transferCheck by Mark 2018/01/09
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    _error
                    );
                #endregion
            }
            else //success (wait 20 min and get data)
            {
                Thread.Sleep(20 * 60 * 1000);
                getA84SFTP();
            }
        }

        /// <summary>
        /// SFTP Get A84檔案
        /// </summary>
        protected void getA84SFTP()
        {
            new FileRelated().createFile(setFile.getC04FilePath());

            string error = string.Empty;
            string error2 = string.Empty;
            string error3 = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getC04FilePath(),
                 setFile.getGZFileName("1"),
                 out error);
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getC04FilePath(),
                 setFile.getGZFileName("2"),
                 out error2);
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getC04FilePath(),
                 setFile.getGZFileName("3"),
                 out error2);
            if (!error.IsNullOrWhiteSpace() || 
                !error2.IsNullOrWhiteSpace() ||
                !error3.IsNullOrWhiteSpace())
            {
                string _error =
                !error.IsNullOrWhiteSpace() ? error :
                !error2.IsNullOrWhiteSpace() ? error2 : error3;
                #region 加入 sql transferCheck by Mark 2018/01/09
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    _error
                    );
                #endregion
            }
            else
            {
                string _sourceFileName = Path.Combine(
                setFile.getC04FilePath(), setFile.getGZFileName("1"));
                string _destFileName = Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName("1"));
                Extension.Decompress(_sourceFileName, _destFileName);

                string _sourceFileName2 = Path.Combine(
                setFile.getC04FilePath(), setFile.getGZFileName("2"));
                string _destFileName2 = Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName("2"));
                Extension.Decompress(_sourceFileName2, _destFileName2);

                string _sourceFileName3 = Path.Combine(
                setFile.getC04FilePath(), setFile.getGZFileName("3"));
                string _destFileName3 = Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName("3"));
                Extension.Decompress(_sourceFileName3, _destFileName3);
                DataToDb();
            }
        }

        /// <summary>
        /// Db save
        /// </summary>
        protected void DataToDb()
        {
            using (IFRS9DBEntities db = new IFRS9DBEntities())
            {
                List<Econ_Foreign> A84Datas = new List<Econ_Foreign>();
                string date = startTime.ToString("yyyyMMdd");
                DateTime _date = startTime.Date;
                TimeSpan _ts = startTime.TimeOfDay;
                #region A84 Data
                var A84s = db.Econ_Foreign.ToList();
                var A84pros = new Econ_Foreign().GetType().GetProperties();
                #region 第一部分
                using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName("1"))))
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
                                if (arr[2] != null && double.TryParse(arr[2], out d) &&
                                    DateTime.TryParseExact(arr[1], "MM/dd/yyyy", null,
                                    System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                                    out dt) && !index.IsNullOrWhiteSpace() &&
                                    DateTime.Now.Date >= dt)
                                {
                                    if (index == "CNFRBAL$ Index") //貿易收支 要排除$
                                        index = "CNFRBAL Index";
                                    index = index.Replace(" ", "_");
                                    var YQ = dt.Year.ToString() + dt.Month.IntToYearQuartly();
                                    var A84 = A84s.Where(x => x.Year_Quartly == YQ).FirstOrDefault();
                                    var A84Data = A84Datas.FirstOrDefault(x => x.Year_Quartly == YQ);
                                    if (A84 != null)
                                    {
                                        setData(A84pros, A84, index, d, date);
                                        A84.LastUpdate_User = _SystemUser;
                                        A84.LastUpdate_Date = _date;
                                        A84.LastUpdate_Time = _ts;
                                    }
                                    else if (A84Data != null)
                                    {
                                        setData(A84pros, A84Data, index, d, date);
                                    }
                                    else
                                    {
                                        Econ_Foreign newData = new Econ_Foreign();
                                        newData.Year_Quartly = YQ;
                                        newData.Create_User = _SystemUser;
                                        newData.Create_Date = _date;
                                        newData.Create_Time = _ts;
                                        setData(A84pros, newData,index,d,date);
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
                #endregion
                #region 第二部分
                using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName("2"))))
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
                                if (arr[2] != null && double.TryParse(arr[2], out d) &&
                                    DateTime.TryParseExact(arr[1], "MM/dd/yyyy", null,
                                    System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                                    out dt) && !index.IsNullOrWhiteSpace() &&
                                    DateTime.Now.Date >= dt)
                                {
                                    index = index.Replace(" ", "_");
                                    var YQ = dt.Year.ToString() + dt.Month.IntToYearQuartly();
                                    var A84 = A84s.Where(x => x.Year_Quartly == YQ).FirstOrDefault();
                                    var A84Data = A84Datas.FirstOrDefault(x => x.Year_Quartly == YQ);
                                    if (A84 != null)
                                    {
                                        setData(A84pros, A84, index, d, date);
                                        A84.LastUpdate_User = _SystemUser;
                                        A84.LastUpdate_Date = _date;
                                        A84.LastUpdate_Time = _ts;
                                    }
                                    else if (A84Data != null)
                                    {
                                        setData(A84pros, A84Data, index, d, date);
                                    }
                                    else
                                    {
                                        Econ_Foreign newData = new Econ_Foreign();
                                        newData.Year_Quartly = YQ;
                                        newData.Create_User = _SystemUser;
                                        newData.Create_Date = _date;
                                        newData.Create_Time = _ts;
                                        setData(A84pros, newData, index, d, date);
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
                #endregion
                #region 第三部分
                using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getC04FilePath(), setFile.getFileName("3"))))
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
                                if (arr[2] != null && double.TryParse(arr[2], out d) &&
                                    DateTime.TryParseExact(arr[1], "MM/dd/yyyy", null,
                                    System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                                    out dt) && !index.IsNullOrWhiteSpace() &&
                                    DateTime.Now.Date >= dt)
                                {
                                    index = index.Replace(" ", "_");
                                    var YQ = dt.Year.ToString() + dt.Month.IntToYearQuartly();
                                    var A84 = A84s.Where(x => x.Year_Quartly == YQ).FirstOrDefault();
                                    var A84Data = A84Datas.FirstOrDefault(x => x.Year_Quartly == YQ);
                                    if (A84 != null)
                                    {
                                        setData(A84pros, A84, index, d, date);
                                        A84.LastUpdate_User = _SystemUser;
                                        A84.LastUpdate_Date = _date;
                                        A84.LastUpdate_Time = _ts;
                                    }
                                    else if (A84Data != null)
                                    {
                                        setData(A84pros, A84Data, index, d, date);
                                    }
                                    else
                                    {
                                        Econ_Foreign newData = new Econ_Foreign();
                                        newData.Year_Quartly = YQ;
                                        newData.Create_User = _SystemUser;
                                        newData.Create_Date = _date;
                                        newData.Create_Time = _ts;
                                        setData(A84pros, newData, index, d, date);
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
                #endregion
                #endregion A84 Data
                #region saveDb
                try
                {
                    db.Econ_Foreign.AddRange(A84Datas);
                    db.SaveChanges();
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
            }
            #endregion saveDb
        }

        private void setData(PropertyInfo[] A84pros, Econ_Foreign data, string index,double d,string date)
        {
            var A84pro = A84pros.Where(x => x.Name == index).FirstOrDefault();
            if (A84pro != null)
            {
                A84pro.SetValue(data, d);
                data.Date = date;
            }
        }
    }
}