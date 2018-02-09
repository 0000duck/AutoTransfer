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
        private static string A96_1Put = ConfigurationManager.AppSettings["A96_1Put"];
        private static string A96_1Get = ConfigurationManager.AppSettings["A96_1Get"];
        private static string A96_2Put = ConfigurationManager.AppSettings["A96_2Put"];
        private static string A96_2Get = ConfigurationManager.AppSettings["A96_2Get"];
        private static string A96_3Put = ConfigurationManager.AppSettings["A96_3Put"];
        private static string A96_3Get = ConfigurationManager.AppSettings["A96_3Get"];
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

        public string getGZFileName()
        {
            return string.Format("Get{0}_{1}.csv.gz",
                  _type.ToString(),
                  _dateTime);
        }

        public string getFileName()
        {
            return string.Format("Get{0}_{1}.csv",
                _type.ToString(),
                _dateTime);
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

        public string putFileName()
        {
            return string.Format("Get{0}_{1}.req",
                _type.ToString(),
                _dateTime);
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

        public string getA96_1FilePath()
        {
            return A96_1Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96_1Get") : A96_1Get;
        }

        public string putA96_1FilePath()
        {
            return A96_1Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96_1Put") : A96_1Put;
        }

        public string getA96_1FileName()
        {
            return string.Format("Get{0}_1_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA96_1FileName()
        {
            return string.Format("Get{0}_1_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA96_2FilePath()
        {
            return A96_2Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96_2Get") : A96_2Get;
        }

        public string putA96_2FilePath()
        {
            return A96_2Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96_2Put") : A96_2Put;
        }

        public string getA96_2FileName()
        {
            return string.Format("Get{0}_2_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA96_2FileName()
        {
            return string.Format("Get{0}_2_{1}.req",
                _type.ToString(),
                _dateTime);
        }

        public string getA96_3FilePath()
        {
            return A96_3Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96_3Get") : A96_3Get;
        }

        public string putA96_3FilePath()
        {
            return A96_3Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96_3Put") : A96_3Put;
        }

        public string getA96_3FileName()
        {
            return string.Format("Get{0}_3_{1}.csv",
                _type.ToString(),
                _dateTime);
        }

        public string putA96_3FileName()
        {
            return string.Format("Get{0}_3_{1}.req",
                                 _type.ToString(),
                                 _dateTime);
        }

        public string getA96_3GZFileName()
        {
            return string.Format("Get{0}_3_{1}.csv.gz",
                  _type.ToString(),
                  _dateTime);
        }
    }
}