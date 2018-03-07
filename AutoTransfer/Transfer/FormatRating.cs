using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class FormatRating
    {
        /// <summary>
        /// 修正 錯誤資料
        /// </summary>
        /// <param name="rating"></param>
        /// <returns></returns>
        public string forRating(string rating, RatingOrg org)
        {
            if (rating.IsNullOrWhiteSpace())
                return string.Empty;
            if (rating.IndexOf("N.A.") > -1)
                return string.Empty;
            if (rating.IndexOf("N.S.") > -1)
                return string.Empty;
            if (rating.IndexOf("N/A") > -1)
                return string.Empty;
            string value = rating.Trim();       
            //if (value.IndexOf("u") > -1)
            //    return value.Split('u')[0].Trim();
            //if (value.IndexOf("e") > -1)
            //    return value.Split('e')[0].Trim();
            if (value.IndexOf("NR") > -1)
                return string.Empty;
            if (value.IndexOf("twNR") > -1)
                return string.Empty;
            if (value.IndexOf("WD") > -1)
                return string.Empty;
            if (((org & RatingOrg.Moody) != RatingOrg.Moody) && (value.IndexOf("WR") > -1))
                return string.Empty;
            if (value.IndexOf("twWR") > -1)
                return string.Empty;

            List<string> splitGetFirst = new List<string>()
            {
                "u","e","/*-","/*+","*-","*+",
                "(bra)","(cl)","(col)","(mex)",
                "(P)","/*","*"
            };
            splitGetFirst.ForEach(x =>
            {
                if (value.Contains(x))
                {
                    value = SplitFirst(value, x);
                }
            });
            return value;
        }

        /// <summary>
        /// 修正 FitchTwn
        /// </summary>
        /// <param name="rating"></param>
        /// <returns></returns>
        public string forRating2(string rating,RatingOrg org)
        {
            if ((org & RatingOrg.FitchTwn) == RatingOrg.FitchTwn)
            {
                if (rating.IsNullOrWhiteSpace())
                    return string.Empty;
                return rating.Split('(')[0].Trim() + "(twn)";
            }
            return rating;
        }

        private string SplitFirst(string value, string splitStr)
        {
            if (value.IsNullOrWhiteSpace())
                return value;
            return value.Split(new string[] { splitStr }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }
    }
}