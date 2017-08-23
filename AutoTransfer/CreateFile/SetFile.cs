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
        private static string startupPath = Directory.GetCurrentDirectory();
        private string _dateTime;
        private TableType _type;

        public SetFile(TableType type, string dateTime)
        {
            _type = type;
            _dateTime = dateTime;
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
    }
}