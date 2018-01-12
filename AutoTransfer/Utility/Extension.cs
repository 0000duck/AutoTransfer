using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AutoTransfer.Utility
{
    public static class EnumUtil
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return System.Enum.GetValues(typeof(T)).Cast<T>();
        }
    }

    public static class Extension
    {
        public static string FormatEquity(this string str)
        {
            if (str.IsNullOrWhiteSpace())
                return string.Empty;
            return str.Split(new string[] { "Equity" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        public static string GetDescription<T>(this T enumerationValue, string title = null, string body = null)
             where T : struct
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException($"{nameof(enumerationValue)} must be of Enum type", nameof(enumerationValue));
            }
            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length > 0)
            {
                var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs.Length > 0)
                {
                    if (!title.IsNullOrWhiteSpace() && !body.IsNullOrWhiteSpace())
                        return string.Format("{0} : {1} => {2}",
                            title,
                            ((DescriptionAttribute)attrs[0]).Description,
                            body
                            );
                    if (!title.IsNullOrWhiteSpace())
                        return string.Format("{0} : {1}",
                            title,
                            ((DescriptionAttribute)attrs[0]).Description
                            );
                    if (!body.IsNullOrWhiteSpace())
                        return string.Format("{0} => {1}",
                            ((DescriptionAttribute)attrs[0]).Description,
                            body
                            );
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            return enumerationValue.ToString();
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static DateTime? StringToDateTimeN(this string str)
        {
            //ex: "08/21/2012"
            if (str.IsNullOrWhiteSpace())
                return null;
            DateTime ratingDateDt = DateTime.MinValue;
            if (DateTime.TryParseExact(str.Trim(), "MM/dd/yyyy", null,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                out ratingDateDt))
                return ratingDateDt;
            return null;
        }

        public static string IntToYearQuartly(this int val)
        {
            string result = string.Empty;
            switch (val)
            {
                case 1:
                case 2:
                case 3:
                    result = "Q1";
                    break;

                case 4:
                case 5:
                case 6:
                    result = "Q2";
                    break;

                case 7:
                case 8:
                case 9:
                    result = "Q3";
                    break;

                case 10:
                case 11:
                case 12:
                    result = "Q4";
                    break;
            }
            return result;
        }

        public static void Decompress(string sourceFileName, string destFileName)
        {
            try
            {
                //被壓縮後的檔案
                FileStream sourceFile = File.OpenRead(sourceFileName);
                //解壓縮後的檔案
                FileStream destFile = File.Create(destFileName);
                //開始
                GZipStream compStream = new GZipStream(sourceFile, CompressionMode.Decompress, true);
                try
                {
                    int theByte = compStream.ReadByte();
                    while (theByte != -1)
                    {
                        destFile.WriteByte((byte)theByte);
                        theByte = compStream.ReadByte();
                    }
                }
                finally
                {
                    compStream.Flush();
                    compStream.Dispose();
                    sourceFile.Flush();
                    sourceFile.Dispose();
                    destFile.Flush();
                    destFile.Dispose();
                }
            }
            catch{ }
        }

        public static string stringToStrSql(this string par)
        {
            if (!par.IsNullOrWhiteSpace())
                return $" '{par.Replace("'", "''")}' ";
            return " null ";
        }

        public static string stringTofloatSql(this string par)
        {
            double d = 0d;
            if (!par.IsNullOrWhiteSpace() && double.TryParse(par.Trim(), out d))
                return d.ToString().stringToStrSql();
            return " null ";
        }

        public static string dateTimeNToStrSql(this DateTime? par)
        {
            if (par.HasValue)
                return par.Value.ToString("yyyy/MM/dd").stringToStrSql();
            return " null ";
        }

        public static string dateTimeToStrSql(this DateTime par)
        {
            return par.ToString("yyyy/MM/dd").stringToStrSql();
        }

        public static string intNToStrSql(this int? par)
        {
            if (par.HasValue)
                return $" {par.Value} ";
            return " null ";
        }

        #region Double? To Double

        /// <summary>
        /// Double? 轉 Double (null 回傳 0d)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double doubleNToDouble(double? value)
        {
            if (value.HasValue)
            {
                return value.Value;
            }

            return 0d;
        }

        #endregion Double? To Double
    }
}