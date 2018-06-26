using AutoTransfer.Abstract;
using AutoTransfer.Commpany;
using AutoTransfer.CreateFile;
using AutoTransfer.Sample;
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
    public class C04
    {
        #region 共用參數

        private Log log = new Log();
        private string logPath = string.Empty;
        private string reportDateStr = string.Empty;
        private ThreadTask t = new ThreadTask();
        private string type = TableType.C04.ToString();
        private DateTime reportDateDt = DateTime.MinValue;
        private DateTime startTime = DateTime.MinValue;
        private string _SystemUser = "System";
        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="year"></param>
        public void startTransfer(string dateTime)
        {
            logPath = log.txtLocation(type);
            DateTime dt = DateTime.Now;
            DateTime _date = dt.Date;
            TimeSpan _ts = dt.TimeOfDay;
            List<string> notParm = new List<string>() { "Year_Quartly", "Date" };

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
                    dt,
                    DateTime.Now,
                    1,
                    logPath,
                   MessageType.DateTime_Format_Fail.GetDescription()
                    );
                #endregion
            }
            else
            {
                var _dt2 = reportDateDt.AddMonths(-18).Year;
                var _m = reportDateDt.AddMonths(-18).Month;
                var _mq = _m.IntToYearQuartly();
                var _dt = reportDateDt.Year;
                List<string> years = new List<string>();
                var _d = _dt - _dt2;
                for (int i = 0; i <= _d; i++)
                {
                    if (i == 0)
                    {
                        _mq.getQuartly().ForEach(x =>
                        {
                            years.Add((_dt2 + i).ToString() + x);
                        });              
                    }
                    else
                    {
                        "Q1".getQuartly().ForEach(x =>
                        {
                            years.Add((_dt2 + i).ToString() + x);
                        });
                    }
                }
                IFRS9DBEntities db = new IFRS9DBEntities();
                var A84datas = new List<Econ_Foreign>();
                foreach (var year in years)
                {
                    A84datas.AddRange(db.Econ_Foreign.AsNoTracking().Where(x => x.Year_Quartly.StartsWith(year)));
                }                  
                if (years.Any() && A84datas.Any())
                {
                    List<Econ_F_YYYYMMDD> C04s = new List<Econ_F_YYYYMMDD>();
                    List<string> yearQuartlys = A84datas.Select(x => x.Year_Quartly).ToList();
                    var A82Datas = db.Moody_Quartly_PD_Info.AsNoTracking().Where(x => yearQuartlys.Contains(x.Year_Quartly)).ToList();
                    var A84pros = new Econ_Foreign().GetType().GetProperties().Where(z => !notParm.Contains(z.Name)).ToList();
                    var C04pros = new Econ_F_YYYYMMDD().GetType().GetProperties().ToList();
                    A84datas.ForEach(x =>
                    {
                        Econ_F_YYYYMMDD C04Data = new Econ_F_YYYYMMDD();
                        C04Data = db.Econ_F_YYYYMMDD.FirstOrDefault(i => i.Year_Quartly == x.Year_Quartly);
                        if (C04Data != null)
                        {
                            var A82Data = A82Datas.FirstOrDefault(z => z.Year_Quartly == x.Year_Quartly);
                            if (A82Data != null)
                                C04Data.PD_Quartly = A82Data.PD;
                            A84pros.ForEach(
                            y =>
                            {
                                var p = C04pros.FirstOrDefault(i => i.Name == y.Name);
                                if (p != null)
                                {
                                    p.SetValue(C04Data, y.GetValue(x));
                                }
                            });
                            C04Data.Processing_Date = dt.ToString("yyyyMMdd");
                            C04Data.LastUpdate_User = _SystemUser;
                            C04Data.LastUpdate_Date = _date;
                            C04Data.LastUpdate_Time = _ts;
                        }
                        else
                        {
                            C04Data = new Econ_F_YYYYMMDD();
                            var A82Data = A82Datas.FirstOrDefault(z => z.Year_Quartly == x.Year_Quartly);
                            if (A82Data != null)
                                C04Data.PD_Quartly = A82Data.PD;
                            A84pros.ForEach(
                                y =>
                                {
                                    var p = C04pros.FirstOrDefault(i => i.Name == y.Name);
                                    if (p != null)
                                    {
                                        p.SetValue(C04Data, y.GetValue(x));
                                    }
                                });
                            C04Data.Processing_Date = dt.ToString("yyyyMMdd");
                            C04Data.Year_Quartly = x.Year_Quartly;
                            C04Data.Create_User = _SystemUser;
                            C04Data.Create_Date = _date;
                            C04Data.Create_Time = _ts;
                            C04s.Add(C04Data);
                        }
                    });
                    db.Econ_F_YYYYMMDD.AddRange(C04s);
                    try
                    {
                        db.SaveChanges();
                        #region 加入 sql transferCheck by Mark 2018/01/09
                        log.bothLog(
                            type,
                            true,
                            reportDateDt,
                            dt,
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
                            dt,
                            DateTime.Now,
                            1,
                            logPath,
                            $"message: {ex.Message}" +
                            $", inner message {ex.InnerException?.InnerException?.Message}"
                            );
                        #endregion
                    }
                    finally
                    {
                        db.Dispose();
                    }
                }
                else
                {
                    #region 加入 sql transferCheck by Mark 2018/01/09
                    log.bothLog(
                        type,
                        false,
                        reportDateDt,
                        dt,
                        DateTime.Now,
                        1,
                        logPath,
                        "找不到A84符合的資料(Econ_Foreign)"
                        );
                    #endregion
                }             
            }
            
        }
    }
}