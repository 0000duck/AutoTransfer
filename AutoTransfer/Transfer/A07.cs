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
            // create ex:GetC03
            if (new CreateA07File().create(tableType, reportDateStr))
            {
                //把資料送給SFTP
                putA07SFTP();
            }
            else
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    "產生檔案失敗");
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
                 setFile.putA07FileName(),
                 out error);

            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    "上傳檔案失敗");
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
                 setFile.getA07FileName(),
                 out error);

            if (!error.IsNullOrWhiteSpace())
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    "下載檔案失敗");
            }
            else
            {
                DataToDb();
            }
        }

        /// <summary>
        /// Db save
        /// </summary>
        protected void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();
            List<Econ_Domestic> A07Data = new List<Econ_Domestic>();

            #region A07 Data

            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getA07FilePath(), setFile.getA07FileName())))
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
                        Econ_Domestic ed = new Econ_Domestic();

                        var arr = line.Split('|');
                        //arr[0]  ex: TWSE Index (台灣證劵交易指數)
                        //arr[1]  ex: 03/31/2016
                        //arr[2]  ex: 8744.83

                        if (arr.Length >= 2)
                        {
                            var A07Info = ed.GetType().GetProperties()
                                          .Where(x => x.Name.Trim().ToLower().Split('_')[0] == arr[0].Trim().ToLower().Split(' ')[0])
                                          .FirstOrDefault();
                            if (A07Info != null)
                            {
                                switch (arr[1].Substring(0, 2))
                                {
                                    case "01":
                                    case "02":
                                    case "03":
                                        ed.Year_Quartly = arr[1].Substring(6, 4) + "Q1";
                                        break;

                                    case "04":
                                    case "05":
                                    case "06":
                                        ed.Year_Quartly = arr[1].Substring(6, 4) + "Q2";
                                        break;

                                    case "07":
                                    case "08":
                                    case "09":
                                        ed.Year_Quartly = arr[1].Substring(6, 4) + "Q3";
                                        break;

                                    case "10":
                                    case "11":
                                    case "12":
                                        ed.Year_Quartly = arr[1].Substring(6, 4) + "Q4";
                                        break;

                                    default:
                                        break;
                                }

                                ed.Date = DateTime.Now.ToString("yyyyMMdd");

                                A07Info.SetValue(ed, double.Parse(arr[2]));

                                A07Data.Add(ed);
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
                for (int i = 0; i < A07Data.Count; i++)
                {
                    string yearQuartly = A07Data[i].Year_Quartly;
                    Econ_Domestic query = db.Econ_Domestic.Where(x => x.Year_Quartly == yearQuartly).FirstOrDefault();

                    if (query == null)
                    {
                        db.Econ_Domestic.Add(
                            new Econ_Domestic
                            {
                                Year_Quartly = A07Data[i].Year_Quartly,
                                Date = A07Data[i].Date,
                                TWSE_Index = A07Data[i].TWSE_Index,
                                TWRGSARP_Index = A07Data[i].TWRGSARP_Index,
                                TWGDPCON_Index = A07Data[i].TWGDPCON_Index,
                                TWLFADJ_Index = A07Data[i].TWLFADJ_Index,
                                TWCPI_Index = A07Data[i].TWCPI_Index,
                                TWMSA1A_Index = A07Data[i].TWMSA1A_Index,
                                TWMSA1B_Index = A07Data[i].TWMSA1B_Index,
                                TWMSAM2_Index = A07Data[i].TWMSAM2_Index,
                                GVTW10YR_Index = A07Data[i].GVTW10YR_Index,
                                TWTRBAL_Index = A07Data[i].TWTRBAL_Index,
                                TWTREXP_Index = A07Data[i].TWTREXP_Index,
                                TWTRIMP_Index = A07Data[i].TWTRIMP_Index,
                                TAREDSCD_Index = A07Data[i].TAREDSCD_Index,
                                TWCILI_Index = A07Data[i].TWCILI_Index,
                                TWBOPCUR_Index = A07Data[i].TWBOPCUR_Index,
                                EHCATW_Index = A07Data[i].EHCATW_Index,
                                TWINDPI_Index = A07Data[i].TWINDPI_Index,
                                TWWPI_Index = A07Data[i].TWWPI_Index,
                                TARSYOY_Index = A07Data[i].TARSYOY_Index,
                                TWEOTTL_Index = A07Data[i].TWEOTTL_Index,
                                SLDETIGT_Index = A07Data[i].SLDETIGT_Index,
                                TWIRFE_Index = A07Data[i].TWIRFE_Index,
                                SINYI_HOUSE_PRICE_index = A07Data[i].SINYI_HOUSE_PRICE_index,
                                CATHAY_ESTATE_index = A07Data[i].CATHAY_ESTATE_index,
                                Real_GDP2011 = A07Data[i].Real_GDP2011,
                                MCCCTW_Index = A07Data[i].MCCCTW_Index,
                                TRDR1T_Index = A07Data[i].TRDR1T_Index
                            }
                       );
                    }
                    else
                    {
                        query.Year_Quartly = (A07Data[i].Year_Quartly.IsNullOrEmpty() == true ? query.Year_Quartly : A07Data[i].Year_Quartly);
                        query.Date = (A07Data[i].Date.IsNullOrEmpty() == true ? query.Date : A07Data[i].Date);
                        query.TWSE_Index = (A07Data[i].TWSE_Index.ToString().IsNullOrEmpty() == true ? query.TWSE_Index : A07Data[i].TWSE_Index);
                        query.TWRGSARP_Index = (A07Data[i].TWRGSARP_Index.ToString().IsNullOrEmpty() == true ? query.TWRGSARP_Index : A07Data[i].TWRGSARP_Index);
                        query.TWGDPCON_Index = (A07Data[i].TWGDPCON_Index.ToString().IsNullOrEmpty() == true ? query.TWGDPCON_Index : A07Data[i].TWGDPCON_Index);
                        query.TWLFADJ_Index = (A07Data[i].TWLFADJ_Index.ToString().IsNullOrEmpty() == true ? query.TWLFADJ_Index : A07Data[i].TWLFADJ_Index);
                        query.TWCPI_Index = (A07Data[i].TWCPI_Index.ToString().IsNullOrEmpty() == true ? query.TWCPI_Index : A07Data[i].TWCPI_Index);
                        query.TWMSA1A_Index = (A07Data[i].TWMSA1A_Index.ToString().IsNullOrEmpty() == true ? query.TWMSA1A_Index : A07Data[i].TWMSA1A_Index);
                        query.TWMSA1B_Index = (A07Data[i].TWMSA1B_Index.ToString().IsNullOrEmpty() == true ? query.TWMSA1B_Index : A07Data[i].TWMSA1B_Index);
                        query.TWMSAM2_Index = (A07Data[i].TWMSAM2_Index.ToString().IsNullOrEmpty() == true ? query.TWMSAM2_Index : A07Data[i].TWMSAM2_Index);
                        query.GVTW10YR_Index = (A07Data[i].GVTW10YR_Index.ToString().IsNullOrEmpty() == true ? query.GVTW10YR_Index : A07Data[i].GVTW10YR_Index);
                        query.TWTRBAL_Index = (A07Data[i].TWTRBAL_Index.ToString().IsNullOrEmpty() == true ? query.TWTRBAL_Index : A07Data[i].TWTRBAL_Index);
                        query.TWTREXP_Index = (A07Data[i].TWTREXP_Index.ToString().IsNullOrEmpty() == true ? query.TWTREXP_Index : A07Data[i].TWTREXP_Index);
                        query.TWTRIMP_Index = (A07Data[i].TWTRIMP_Index.ToString().IsNullOrEmpty() == true ? query.TWTRIMP_Index : A07Data[i].TWTRIMP_Index);
                        query.TAREDSCD_Index = (A07Data[i].TAREDSCD_Index.ToString().IsNullOrEmpty() == true ? query.TAREDSCD_Index : A07Data[i].TAREDSCD_Index);
                        query.TWCILI_Index = (A07Data[i].TWCILI_Index.ToString().IsNullOrEmpty() == true ? query.TWCILI_Index : A07Data[i].TWCILI_Index);
                        query.TWBOPCUR_Index = (A07Data[i].TWBOPCUR_Index.ToString().IsNullOrEmpty() == true ? query.TWBOPCUR_Index : A07Data[i].TWBOPCUR_Index);
                        query.EHCATW_Index = (A07Data[i].EHCATW_Index.ToString().IsNullOrEmpty() == true ? query.EHCATW_Index : A07Data[i].EHCATW_Index);
                        query.TWINDPI_Index = (A07Data[i].TWINDPI_Index.ToString().IsNullOrEmpty() == true ? query.TWINDPI_Index : A07Data[i].TWINDPI_Index);
                        query.TWWPI_Index = (A07Data[i].TWWPI_Index.ToString().IsNullOrEmpty() == true ? query.TWWPI_Index : A07Data[i].TWWPI_Index);
                        query.TARSYOY_Index = (A07Data[i].TARSYOY_Index.ToString().IsNullOrEmpty() == true ? query.TARSYOY_Index : A07Data[i].TARSYOY_Index);
                        query.TWEOTTL_Index = (A07Data[i].TWEOTTL_Index.ToString().IsNullOrEmpty() == true ? query.TWEOTTL_Index : A07Data[i].TWEOTTL_Index);
                        query.SLDETIGT_Index = (A07Data[i].SLDETIGT_Index.ToString().IsNullOrEmpty() == true ? query.SLDETIGT_Index : A07Data[i].SLDETIGT_Index);
                        query.TWIRFE_Index = (A07Data[i].TWIRFE_Index.ToString().IsNullOrEmpty() == true ? query.TWIRFE_Index : A07Data[i].TWIRFE_Index);
                        query.SINYI_HOUSE_PRICE_index = (A07Data[i].SINYI_HOUSE_PRICE_index.ToString().IsNullOrEmpty() == true ? query.SINYI_HOUSE_PRICE_index : A07Data[i].SINYI_HOUSE_PRICE_index);
                        query.CATHAY_ESTATE_index = (A07Data[i].CATHAY_ESTATE_index.ToString().IsNullOrEmpty() == true ? query.CATHAY_ESTATE_index : A07Data[i].CATHAY_ESTATE_index);
                        query.Real_GDP2011 = (A07Data[i].Real_GDP2011.ToString().IsNullOrEmpty() == true ? query.Real_GDP2011 : A07Data[i].Real_GDP2011);
                        query.MCCCTW_Index = (A07Data[i].MCCCTW_Index.ToString().IsNullOrEmpty() == true ? query.MCCCTW_Index : A07Data[i].MCCCTW_Index);
                        query.TRDR1T_Index = (A07Data[i].TRDR1T_Index.ToString().IsNullOrEmpty() == true ? query.TRDR1T_Index : A07Data[i].TRDR1T_Index);
                    }

                    db.SaveChanges();
                }

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