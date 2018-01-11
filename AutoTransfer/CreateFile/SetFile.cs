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
        private static string A93Put = ConfigurationManager.AppSettings["A93Put"];
        private static string A93Get = ConfigurationManager.AppSettings["A93Get"];
        private static string A96Put = ConfigurationManager.AppSettings["A96Put"];
        private static string A96Get = ConfigurationManager.AppSettings["A96Get"];
        private static string name = ConfigurationManager.AppSettings["FIRMNAME"];
        private static string flag = ConfigurationManager.AppSettings["PROGRAMFLAG"];
        private static string startupPath = Directory.GetCurrentDirectory();
        private string _dateTime;

        private TableType _type;

        public SetFile(TableType type, string dateTime)
        {
            _type = type;
            _dateTime = dateTime;
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

        public string getA96FilePath()
        {
            return A96Get.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96Get") : A96Get;
        }

        public string putA96FilePath()
        {
            return A96Put.IsNullOrWhiteSpace() ?
                Path.Combine(startupPath, "A96Put") : A96Put;
        }
    }
}