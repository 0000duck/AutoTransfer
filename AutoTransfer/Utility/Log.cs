using System;
using System.Configuration;
using System.IO;

namespace AutoTransfer.Utility
{
    public class Log
    {
        #region save txtlog
        /// <summary>
        /// 寫入 Txt Log
        /// </summary>
        /// <param name="tableName">table名</param>
        /// <param name="falg">成功或失敗</param>
        /// <param name="start">開始時間</param>
        /// <param name="folderPath">檔案路徑</param>
        /// <param name="detail">內容</param>
        public void txtLog(string tableName, bool falg, DateTime start, string folderPath,string detail = null)
        {
                string txtData = string.Empty;
                try //試著抓取舊資料
                {
                    txtData = File.ReadAllText(folderPath);
                }
                catch { }
                string txt = string.Format("{0}_{1}_{2}{3}",
                             tableName,
                             start.ToString("yyyyMMddHHmmss"),
                             falg ? "Y" : "N",
                             !detail.IsNullOrWhiteSpace() ?
                             string.Format(" => {0}", detail) :
                             string.Empty
                             );
                if (!string.IsNullOrWhiteSpace(txtData)) //有舊資料就換行寫入下一筆
                {
                    txtData += string.Format("\r\n{0}", txt);
                }
                else //沒有就直接寫入
                {
                    txtData = txt;
                }
                FileStream fs = new FileStream(folderPath, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                sw.Write(txtData); //存檔
                sw.Close();
        }
        #endregion

        public string txtLocation(string fileName)
        {
            string startupPath = Directory.GetCurrentDirectory();

            string txtLog = "txtLog";

            string configFileName = ConfigurationManager.AppSettings["txtLocation"];
            if (!string.IsNullOrWhiteSpace(configFileName))
                txtLog = configFileName; //config 設定就取代

            string txtPath = Path.Combine(startupPath, txtLog);

            new FileRelated().createFile(txtPath);
            if (!fileName.EndsWith(".txt"))
                fileName = string.Format("{0}.txt", fileName);
            return Path.Combine(txtPath, fileName);
        }
    }
}
