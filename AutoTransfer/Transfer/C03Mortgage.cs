using AutoTransfer.Utility;
using System;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class C03Mortgage
    {
        #region 共用參數
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private string type = TableType.C03Mortgage.ToString();
        #endregion 共用參數

        #region
        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public void startTransfer(string dateTime)
        {
            logPath = log.txtLocation(type);

            IFRS9Entities db = new IFRS9Entities();
            startTime = DateTime.Now;

            if (dateTime.Length != 8 ||
               !DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
               System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt))
            {
                db.Dispose();
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
                string yearString = dateTime.Substring(0, 4);

                switch (dateTime.Substring(4, 2))
                {
                    case "04":
                        new CompleteEvent().saveC03Mortgage(yearString + "Q1");
                        break;
                    case "07":
                        new CompleteEvent().saveC03Mortgage(yearString + "Q2");
                        break;
                    case "10":
                        new CompleteEvent().saveC03Mortgage(yearString + "Q3");
                        break;
                    case "01":
                        new CompleteEvent().saveC03Mortgage((int.Parse(yearString)-1).ToString() + "Q4");
                        break;
                    default:
                        db.Dispose();
                        log.txtLog(
                            type,
                            false,
                            startTime,
                            logPath,
                            "排程執行月份必須是 04,07,10,01 月"
                        );
                        break;
                }
            }
        }
        #endregion

    }
}