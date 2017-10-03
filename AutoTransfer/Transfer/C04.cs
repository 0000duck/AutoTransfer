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
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private int verInt = 0;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.C04;
        private string type = TableType.C04.ToString();

        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="year"></param>
        public void startTransfer(string year)
        {
            IFRS9Entities db = new IFRS9Entities();
            DateTime dt = DateTime.Now;
            if (!year.IsNullOrWhiteSpace())
            {
                var A84datas = db.Econ_Foreign.AsNoTracking()
                    .Where(x => x.Year_Quartly.StartsWith(year));
                if (A84datas.Any())
                {
                    List<Econ_F_YYYYMMDD> C04s = new List<Econ_F_YYYYMMDD>();
                    List<string> yearQuartlys = A84datas.Select(x=>x.Year_Quartly).ToList();
                    var A82Datas = db.Moody_Quartly_PD_Info.AsNoTracking().Where(x=> yearQuartlys.Contains(x.Year_Quartly)).ToList();
                    A84datas.ToList().ForEach(x =>
                    {
                        Econ_F_YYYYMMDD C04Data = new Econ_F_YYYYMMDD();
                        C04Data.Processing_Date = dt.ToString("yyyyMMdd");
                        C04Data.Year_Quartly = x.Year_Quartly;
                        var A82Data = A82Datas.FirstOrDefault(z => z.Year_Quartly == x.Year_Quartly);
                        if (A82Data != null)
                            C04Data.PD_Quartly = A82Data.PD;
                        List<string> notParm = new List<string>() { "Year_Quartly", "Date" };
                        x.GetType().GetProperties().Where(z=> !notParm.Contains(z.Name)).ToList().ForEach(
                            y =>
                            {
                                var p = C04Data.GetType().GetProperties().FirstOrDefault(i => i.Name == y.Name);
                                if (p != null)
                                {
                                    p.SetValue(C04Data, y.GetValue(x));
                                }
                            });
                        C04s.Add(C04Data);
                    });                                               
                    db.Econ_F_YYYYMMDD.AddRange(C04s);
                    try
                    {
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
                }
            }
        }
    }
}