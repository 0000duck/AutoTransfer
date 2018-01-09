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
                new CompleteEvent().saveC03Mortgage(dateTime);
            }
        }
        #endregion

    }
}