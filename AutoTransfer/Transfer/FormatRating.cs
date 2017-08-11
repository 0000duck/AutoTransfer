using AutoTransfer.Utility;
using System;

namespace AutoTransfer.Transfer
{
    public class FormatRating
    {
        /// <summary>
        /// 修正 MOODY'S ISSUE 錯誤資料
        /// </summary>
        /// <param name="rating"></param>
        /// <returns></returns>
        public string Moodys(string rating) 
        {
            return rating.IsNullOrWhiteSpace() ? string.Empty :
                rating.Split(new string[] { "*-" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        /// <summary>
        /// 修正FITCH ISSUER 錯誤資料
        /// </summary>
        /// <param name="rating"></param>
        /// <returns></returns>
        public string Flight(string rating)
        {
            if (rating.IsNullOrWhiteSpace())
                return string.Empty;
            string value = rating.Trim();
            if (value.IndexOf("%twn%") > -1)
                return string.Empty;
            if ("#N/A N/A".Equals(value))
                return string.Empty;
            if ("BBBu".Equals(value))
                return "BBB";
            if ("A-u".Equals(value) || "A+u".Equals(value))
                return "A";
            return value;
        }
    }
}
