using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class FormatRating
    {
        /// <summary>
        /// 修正 錯誤資料
        /// </summary>
        /// <param name="rating">評等</param>
        /// <param name="org">RatingOrg</param>
        /// <param name="rule_1">規則01 : 置換特殊字元成空值</param>
        /// <param name="rule_2">規則02 : 以新值取代舊值 </param>
        /// <returns></returns>
        public string forRating(string rating, RatingOrg org,List<string> rule_1, List<Tuple<string, string>> rule_2)
        {
            if (rating.IsNullOrWhiteSpace())
                return string.Empty;
            string value = rating.Trim();
            //if (rating.IndexOf("N.A.") > -1)
            //    return string.Empty;
            //if (rating.IndexOf("N.S.") > -1)
            //    return string.Empty;
            //if (rating.IndexOf("N/A") > -1)
            //    return string.Empty;
            //if (value.IndexOf("NR") > -1)
            //    return string.Empty;
            //if (value.IndexOf("twNR") > -1)
            //    return string.Empty;
            //if (value.IndexOf("WD") > -1)
            //    return string.Empty;
            if (((org & RatingOrg.Moody) != RatingOrg.Moody) && (value.IndexOf("WR") > -1))
                return string.Empty;
            //if (value.IndexOf("twWR") > -1)
            //    return string.Empty;

            //List<string> splitGetFirst = new List<string>()
            //{
            //    "u","e","/*-","/*+","*-","*+",
            //    "(bra)","(cl)","(col)","(mex)",
            //    "(P)","/*","*"
            //};

            
            List<string> Liststrs = new List<string>();
            rule_1.ForEach(x =>
            {
                if (value.Contains(x))
                {
                    Liststrs.Add(x);
                    //value = value.Replace(x,string.Empty);
                }
            });
            if (Liststrs.Count > 0)
            {
                string[] strs = Liststrs.ToArray();
                ExchangeSort(strs);
                strs.Reverse().ToList().ForEach(x =>
                {
                    value = value.Replace(x, string.Empty);
                });
            }
            rule_2.ForEach(x =>
            {
                if (value.Contains(x.Item1) && !x.Item2.IsNullOrWhiteSpace())
                {
                    value = value.Replace(x.Item1, x.Item2);
                }
            });
            return value.Trim();
        }

        //Exchange Sort(交換排序法)
        public static void ExchangeSort(string[] list)
        {
            int n = list.Length;
            string temp;
            for (int i = 0; i <= n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (list[i].Contains(list[j]))
                    {  // 比較鄰近兩個物件，左邊包含右邊時就互換。	       
                        temp = list[j];
                        list[j] = list[i];
                        list[i] = temp;
                    }
                }
            }
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