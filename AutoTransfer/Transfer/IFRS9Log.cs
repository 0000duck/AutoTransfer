using AutoTransfer.Utility;
using System;
using static AutoTransfer.Enum.Ref;
using System.IO.Compression;
using System.IO;

namespace AutoTransfer.Transfer
{
    public class IFRS9Log
    {
        #region 共用參數
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private DateTime startTime = DateTime.MinValue;
        private EmailRelated emailRelated = new EmailRelated();
        private string type = TableType.IFRS9Log.ToString();
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
                try
                {
                    string nowDate = DateTime.Now.ToString("yyyyMMdd");

                    string startPath = $@"D:\IFRS9Log{nowDate}";

                    new FileRelated().createFile(startPath);

                    string soureFileName1 = @"D:\IFRS9Log\LogOutput.txt";
                    string soureFileName2 = @"D:\IFRS9Log\LogScheduleOutput.txt";

                    if (File.Exists(soureFileName1) == true)
                    {
                        File.Copy(soureFileName1, $@"{startPath}\LogOutput.txt", true);
                    }

                    if (File.Exists(soureFileName2) == true)
                    {
                        File.Copy(soureFileName2, $@"{startPath}\LogScheduleOutput.txt", true);
                    }

                    string zipFileName = $"IFRS9Log{nowDate}.zip";

                    string zipPath = $@"D:\{zipFileName}";

                    File.Delete(zipPath);

                    ZipFile.CreateFromDirectory(startPath, zipPath);

                    emailRelated.SendEmail(zipFileName, zipFileName, zipPath);

                    try
                    {
                        File.Delete(soureFileName1);
                        File.Delete(soureFileName2);
                    }
                    catch (Exception ex)
                    {
                    }

                    log.bothLog(
                        type,
                        true,
                        reportDateDt,
                        startTime,
                        DateTime.Now,
                        1,
                        logPath,
                        "寄送完成"
                    );
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
                        ex.Message
                    );
                }
            }
        }
    }
}