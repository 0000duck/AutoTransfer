using System;
using System.ComponentModel;

namespace AutoTransfer.Utility
{
    public static class Extension
    {
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static DateTime? StringToDateTimeN(this string str)
        {
            //ex: "08/21/2012"
            if (str.IsNullOrWhiteSpace())
                return null;
            DateTime ratingDateDt = DateTime.MinValue;
            if (!DateTime.TryParseExact(str.Trim(), "MM/dd/yyyy",null,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces, 
                out ratingDateDt))
                return ratingDateDt;
            return null;
        }

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

    }
}
