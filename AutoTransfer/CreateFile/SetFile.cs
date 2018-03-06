using AutoTransfer.Utility;
using System.Configuration;
using System.IO;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class SetFile
    {

        private static string commpanyGet = ConfigurationManager.AppSettings["commpanyGet"];
        private static string commpanyPut = ConfigurationManager.AppSettings["commpanyPut"];
        private static string sampleGet = ConfigurationManager.AppSettings["sampleGet"];
        private static string samplePut = ConfigurationManager.AppSettings["samplePut"];
        private static string getC04Get = ConfigurationManager.AppSettings["getC04Get"];
        private static string getC04Put = ConfigurationManager.AppSettings["getC04Put"];
        private static string A07Put = ConfigurationManager.AppSettings["A07Put"];
        private static string A07Get = ConfigurationManager.AppSettings["A07Get"];
        private static string A91Put = ConfigurationManager.AppSettings["A91Put"];
        private static string A91Get = ConfigurationManager.AppSettings["A91Get"];
        private static string A92Put = ConfigurationManager.AppSettings["A92Put"];
        private static string A92Get = ConfigurationManager.AppSettings["A92Get"];
        private static string A93Put = ConfigurationManager.AppSettings["A93Put"];
        private static string A93Get = ConfigurationManager.AppSettings["A93Get"];
        private static string A9611Put = ConfigurationManager.AppSettings["A9611Put"];
        private static string A9611Get = ConfigurationManager.AppSettings["A9611Get"];
        private static string A9612Put = ConfigurationManager.AppSettings["A9612Put"];
        private static string A9612Get = ConfigurationManager.AppSettings["A9612Get"];
        private static string A9613Put = ConfigurationManager.AppSettings["A9613Put"];
        private static string A9613Get = ConfigurationManager.AppSettings["A9613Get"];
        private static string A9621Put = ConfigurationManager.AppSettings["A9621Put"];
        private static string A9621Get = ConfigurationManager.AppSettings["A9621Get"];
        private static string A9622Put = ConfigurationManager.AppSettings["A9622Put"];
        private static string A9622Get = ConfigurationManager.AppSettings["A9622Get"];
        private static string SecurityDesPut = string.Empty;
        private static string SecurityDesGet = string.Empty;
        private static string name = ConfigurationManager.AppSettings["FIRMNAME"];
        private static string flag = ConfigurationManager.AppSettings["PROGRAMFLAG"];
        private static string userNumber = ConfigurationManager.AppSettings["USERNUMBER"];
        private static string WS = ConfigurationManager.AppSettings["WS"];
        private static string SN = ConfigurationManager.AppSettings["SN"];
        private static string startupPath = Directory.GetCurrentDirectory();
        private string _dateTime;

        private TableType _type;

        public SetFile(TableType type, string dateTime)
        {
            _type = type;
            _dateTime = dateTime;
        }

        public string getUSERNUMBER()
        {
            return userNumber;
        }

        public string getWS()
        {
            return WS;
        }

        public string getSN()
        {
            return SN;
        }

        public string getFIRMNAME()
        {
            return name;
        }

        public string getPROGRAMFLAG()
        {
            return flag;
        }

        public string getGZFileName(string str = null)
        {
            return string.Format("Get{0}_{1}{2}.csv.gz",
                  _type.ToString(),
                  _dateTime,
                  getExtensionName(str)
                  );
        }

        public string getFileName(string str = null)
        {
            return string.Format("Get{0}_{1}{2}.csv",
                _type.ToString(),
                _dateTime,
                getExtensionName(str));
        }

        public string getC04FilePath()
        {
            return getC04Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "C04Get") : getC04Get;
        }

        public string getSecurityDesFileName()
        {
            return string.Format("securityDes_{0}.csv",
                _dateTime);
        }

        public string getSecurityDesFilePath()
        {
            return SecurityDesGet.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "securityDesGet") : SecurityDesGet;
        }

        public string getCommpanyFileName()
        {
            return string.Format("commpany{0}_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string getCommpanyFilePath()
        {
            return commpanyGet.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "commpanyGet") : commpanyGet;
        }

        public string getSampleFileName()
        {
            return string.Format("sample{0}_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string getSampleFilePath()
        {
            return sampleGet.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "sampleGet") : sampleGet;
        }

        public string putFileName(string str = null)
        {
            return string.Format("Get{0}_{1}{2}.req",
                _type.ToString(),
                _dateTime,
                getExtensionName(str));
        }

        public string putC04FilePath()
        {
            return getC04Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "C04Put") : getC04Put;
        }

        public string putSecurityDesFileName()
        {
            return string.Format("securityDes_{0}.req",
                _dateTime);
        }

        public string putSecurityDesFilePath()
        {
            return SecurityDesPut.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "securityDesPut") : SecurityDesPut;
        }

        public string putCommpanyFileName()
        {
            return string.Format("commpany{0}_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string putCommpanyFilePath()
        {
            return commpanyPut.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "commpanyPut") : commpanyPut;
        }

        public string putSampleFileName()
        {
            return string.Format("sample{0}_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string putSampleFilePath()
        {
            return samplePut.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "samplePut") : samplePut;
        }

        public string getA07FilePath()
        {
            return A07Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A07Get") : A07Get;
        }

        public string putA07FilePath()
        {
            return A07Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A07Put") : A07Put;
        }

        public string getA91FilePath()
        {
            return A91Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A91Get") : A91Get;
        }

        public string putA91FilePath()
        {
            return A91Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A91Put") : A91Put;
        }

        public string getA91FileName()
        {
            return string.Format("Get{0}_{1}y.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA91FileName()
        {
            return string.Format("Get{0}_{1}y.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA91GZFileName()
        {
            return string.Format("Get{0}_{1}y.csv.gz",
                  _type.ToString(),
                  _dateTime);
        }

        public string getA92FilePath()
        {
            return A92Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A92Get") : A92Get;
        }

        public string putA92FilePath()
        {
            return A92Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A92Put") : A92Put;
        }

        public string getA92FileName()
        {
            return string.Format("Get{0}_{1}q.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA92FileName()
        {
            return string.Format("Get{0}_{1}q.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA92GZFileName()
        {
            return string.Format("Get{0}_{1}q.csv.gz",
                  _type.ToString(),
                  _dateTime);
        }

        public string getA93FilePath()
        {
            return A93Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A93Get") : A93Get;
        }

        public string putA93FilePath()
        {
            return A93Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A93Put") : A93Put;
        }

        public string getA93FileName()
        {
            return string.Format("Get{0}_{1}m.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA93FileName()
        {
            return string.Format("Get{0}_{1}m.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA93GZFileName()
        {
            return string.Format("Get{0}_{1}m.csv.gz",
                  _type.ToString(),
                  _dateTime);
        }

        public string getA9611FilePath()
        {
            return A9611Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9611Get") : A9611Get;
        }

        public string putA9611FilePath()
        {
            return A9611Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9611Put") : A9611Put;
        }

        public string getA9611FileName()
        {
            return string.Format("Get{0}1_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA9611FileName()
        {
            return string.Format("Get{0}1_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA9611GZFileName()
        {
            return string.Format("Get{0}1_{1}.csv.gz",
                                  _type.ToString(),
                                  _dateTime);
        }

        public string getA9612FilePath()
        {
            return A9612Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9612Get") : A9612Get;
        }

        public string putA9612FilePath()
        {
            return A9612Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9612Put") : A9612Put;
        }

        public string getA9612FileName()
        {
            return string.Format("Get{0}2_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA9612FileName()
        {
            return string.Format("Get{0}2_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA9613FilePath()
        {
            return A9613Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9613Get") : A9613Get;
        }

        public string putA9613FilePath()
        {
            return A9613Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9613Put") : A9613Put;
        }

        public string getA9613FileName()
        {
            return string.Format("Get{0}3_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA9613FileName()
        {
            return string.Format("Get{0}3_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA9621FilePath()
        {
            return A9621Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9621Get") : A9621Get;
        }

        public string putA9621FilePath()
        {
            return A9621Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9621Put") : A9621Put;
        }

        public string getA9621FileName()
        {
            return string.Format("Get{0}1_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA9621FileName()
        {
            return string.Format("Get{0}1_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA9621GZFileName()
        {
            return string.Format("Get{0}1_{1}.csv.gz",
                                  _type.ToString(),
                                  _dateTime);
        }

        public string getA9622FilePath()
        {
            return A9622Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9622Get") : A9622Get;
        }

        public string putA9622FilePath()
        {
            return A9622Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A9622Put") : A9622Put;
        }

        public string getA9622FileName()
        {
            return string.Format("Get{0}2_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA9622FileName()
        {
            return string.Format("Get{0}2_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA9622GZFileName()
        {
            return string.Format("Get{0}2_{1}.csv.gz",
                                  _type.ToString(),
                                  _dateTime);
        }

        private string getExtensionName(string str)
        {
            return str.IsNullOrWhiteSpace() ? string.Empty : $"_{str}";
        }
    }
}