using AutoTransfer.Utility;
using System;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class FormatRating
    {
        /// <summary>
        /// 修正 MOODY'S ISSUE 錯誤資料
        /// </summary>
        /// <param name="rating"></param>
        /// <returns></returns>
        public string forRating(string rating) 
        {
            if (rating.IsNullOrWhiteSpace())
                return string.Empty;
            string value = rating.Trim();
            if ("BBBu".Equals(value))
                return "BBB";
            if ("A-u".Equals(value) || "A+u".Equals(value))
                return "A";
            if (value.IndexOf("/*-") > -1)
                return value.Split(new string[] { "/*-" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (value.IndexOf("*-") > -1)
                return value.Split(new string[] { "*-" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return value;
        }

        /// <summary>
        /// 修正FITCH ISSUER 錯誤資料
        /// </summary>
        /// <param name="rating"></param>
        /// <param name="org"></param>
        /// <param name="Bonds"></param>
        /// <returns></returns>
        public string Flight(string rating, RatingOrg org,bool Bonds)
        {
            if (rating.IsNullOrWhiteSpace())
                return string.Empty;
            string value = rating.Trim();

            if (!Bonds)
            {
                //*修正 S&P ISSUER 錯誤資料*
                //CURR_SP_ISSUE AS CURR_SP_ISSUE1,
                if (org.Equals(RatingOrg.SP))
                {
                    return value;
                }
                //*修正 MOODY'S ISSUE 錯誤資料*
                //CASE WHEN  CURR_MOODYS_ISSUE = 'Baa2 *-' THEN  'Baa2'
                //ELSE CURR_MOODYS_ISSUE END AS  CURR_MOODYS_ISSUE1,
                if (org.Equals(RatingOrg.Moody))
                {
                    if ("Baa2 *-".Equals(value))
                    {
                        return "Baa2";
                    }
                    return value;
                }
                //*修正FITCH ISSUE 錯誤資料*
                //CASE WHEN CURR_FITCH_ISSUE like '%twn%' THEN ''
                //WHEN CURR_FITCH_ISSUE = '#N/A N/A' THEN  ''
                //ELSE CURR_FITCH_ISSUE END AS CURR_FITCH_ISSUE1,
                if (org.Equals(RatingOrg.Fitch))
                {
                    if (value.IndexOf("twn") > -1)
                        return string.Empty;
                    if ("#N/A N/A".Equals(value))
                        return string.Empty;
                    return value;
                }
                //*修正 FITCH ISSUE-->惠譽台灣*
                //CASE WHEN CURR_FITCH_ISSUE like '%twn%' THEN CURR_FITCH_ISSUE
                //ELSE '' END AS CURR_FITCH_TW_ISSUE1,
                if (org.Equals(RatingOrg.FitchTwn))
                {
                    if (value.IndexOf("twn") > -1)
                        return value;
                    return string.Empty;
                }
            }

            if (Bonds)
            {
                //*修正 S&P ISSUER 錯誤資料*
                //CURR_SP_ISSUE AS CURR_SP_ISSUE1,
                if (org.Equals(RatingOrg.SP))
                {
                    return rating;
                }
                //*修正MOODY'S ISSUER 錯誤資料*
                //CASE WHEN CURR_MOODYS_ISSUER = 'Baa2 /*-' THEN 'Baa2'
                //ELSE CURR_MOODYS_ISSUER END AS CURR_MOODYS_ISSUER1,
                if (org.Equals(RatingOrg.Moody))
                {
                    if ("Baa2 *-".Equals(value))
                    {
                        return "Baa2";
                    }
                    return value;
                }
                //*修正FITCH ISSUER 錯誤資料*
                //CASE WHEN CURR_FITCH_ISSUER like '%twn%' THEN ''
                //WHEN CURR_FITCH_ISSUER = '#N/A N/A' THEN ''
                //WHEN CURR_FITCH_ISSUER = 'BBBu' THEN 'BBB'
                //WHEN CURR_FITCH_ISSUER = 'A-u' THEN 'A'
                //WHEN CURR_FITCH_ISSUER = 'A+u' THEN 'A'
                //ELSE CURR_FITCH_ISSUER END AS CURR_FITCH_ISSUER1,
                if (org.Equals(RatingOrg.Fitch))
                {
                    if (value.IndexOf("twn") > -1)
                        return string.Empty;
                    if ("#N/A N/A".Equals(value))
                        return string.Empty;
                    if ("BBBu".Equals(value))
                        return "BBB";
                    if ("A-u".Equals(value) || "A+u".Equals(value))
                        return "A";
                    return value;
                }
                //*修正FITCH ISSUER-->  惠譽台灣*
                //CASE WHEN CURR_FITCH_ISSUER like '%twn%' THEN CURR_FITCH_ISSUER 
                //ELSE ' ' END AS CURR_FITCH_TW_ISSUER1
                if (org.Equals(RatingOrg.FitchTwn))
                {
                    if (value.IndexOf("twn") > -1)
                        return value;
                    return string.Empty;
                }
            }

            return value;
        }
    }
}
