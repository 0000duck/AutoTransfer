using AutoTransfer.Abstract;
using AutoTransfer.Commpany;
using AutoTransfer.CreateFile;
using AutoTransfer.Sample;
using AutoTransfer.SFTPConnect;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class A96
    {
        #region 共用參數
        private FormatRating fr = new FormatRating();

        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private int verInt = 0;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A96;
        private string type = TableType.A96.ToString();
        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public void startTransfer(string dateTime)
        {
            IFRS9Entities db = new IFRS9Entities();
            startTime = DateTime.Now;

            if (dateTime.Length != 8 ||
               !DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
               System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt))
            {
                db.Dispose();
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

            var A41 = db.Bond_Account_Info.AsNoTracking()
                        .Any(x => x.Report_Date == reportDateDt);
            verInt = db.Bond_Account_Info
                       .Where(x => x.Report_Date == reportDateDt && x.Version != null)
                       .DefaultIfEmpty().Max(x => x.Version == null ? 0 : x.Version.Value);
            
            logPath = log.txtLocation(type);
            if (!A41 || verInt == 0)
            {
                db.Dispose();
                List<string> errs = new List<string>();

                if (!A41)
                {
                    errs.Add(MessageType.not_Find_Any.GetDescription("A41"));
                }

                if (verInt == 0)
                {
                    errs.Add(MessageType.transferError.GetDescription());
                }

                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    string.Join(",", errs)
                    );
            }
            else
            {
                db.Dispose();
                reportDateStr = dateTime;
                setFile = new SetFile(tableType, dateTime);
                createA96_1File();
            }
        }

        protected void createA96_1File()
        {
            if (new CreateA96_1File().create(tableType, reportDateStr, verInt))
            {
                //把資料送給SFTP
                putA96_1SFTP();
            }
            else
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    MessageType.Create_File_Fail.GetDescription(type)
                );
            }
        }

        protected void putA96_1SFTP()
        {
            string error = string.Empty;

            //new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            //    .Put(string.Empty,
            //     setFile.putA96_1FilePath(),
            //     setFile.putA96_1FileName(),
            //     out error);

            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    verInt,
                    logPath,
                    error
                );
            }
            else //success (wait 20 min and get data)
            {
                //Thread.Sleep(20 * 60 * 1000);
                //getSampleSFTP();
            }
        }
    }
}
