﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Utility
{
    public class Log
    {
        #region save txtlog

        /// <summary>
        /// 寫入 Txt Log
        /// </summary>
        /// <param name="tableName">table名</param>
        /// <param name="flag">成功或失敗</param>
        /// <param name="start">開始時間</param>
        /// <param name="folderPath">檔案路徑</param>
        /// <param name="detail">內容</param>
        public void txtLog(string tableName, bool flag, DateTime start, string folderPath, string detail = null)
        {
            string txtData = string.Empty;
            try //試著抓取舊資料
            {
                txtData = File.ReadAllText(folderPath, System.Text.Encoding.Default);
            }
            catch { }
            string txt = string.Format("{0}_{1}_{2}{3}",
                         tableName,
                         start.ToString("yyyyMMddHHmmss"),
                         flag ? "Y" : "N",
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

        #endregion save txtlog

        /// <summary>
        /// 執行 txtlog & sql log
        /// </summary>
        /// <param name="tableName">table名</param>
        /// <param name="flag">成功或失敗</param>
        /// <param name="reportDate">基準日</param>
        /// <param name="start">開始時間</param>
        /// <param name="end">結束時間</param>
        /// <param name="version">版本</param>
        /// <param name="folderPath">檔案路徑</param>
        /// <param name="detail">內容</param>
        public void bothLog(string tableName, bool flag, DateTime reportDate, DateTime start, DateTime end, int version, string folderPath, string detail = null)
        {
            saveTransferCheck(tableName, flag, reportDate, version, start, end);
            if (tableName == "C03")
                tableName = TableType.C03Mortgage.ToString();
            txtLog(tableName, flag, start, folderPath, detail);
        }

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

        /// <summary>
        /// 判斷轉檔紀錄是否有存在
        /// </summary>
        /// <param name="fileNames">檔案名稱(這次要轉檔的名稱)</param>
        /// <param name="checkName">要判斷的檔案名稱(執行這次轉檔前要完成的動作的檔案名稱)</param>
        /// <param name="reportDate">基準日</param>
        /// <param name="version">版本</param>
        /// <returns></returns>
        public bool checkTransferCheck(
            string fileName,
            string checkName,
            DateTime reportDate,
            int version)
        {
            if (fileName.IsNullOrWhiteSpace() || checkName.IsNullOrWhiteSpace())
                return false;
            using (IFRS9Entities db = new IFRS9Entities())
            {
                var checkTable = db.Transfer_CheckTable.AsNoTracking();
                //須符合有一筆"Y"(上一部完成),前置動作檢查檔案為A53版本只會有一版
                if (checkTable.Any(x => x.ReportDate == reportDate &&
                                                    ((checkName == "A53" &&
                                                     x.Version == 1) ||
                                                    (x.File_Name == checkName &&
                                                   x.Version == version)) &&
                                                   x.TransferType == "Y") &&
                //自己沒有"Y"(重複做) 才算符合,轉檔為A53不用判斷
                    (fileName == "A53" ||
                    !checkTable.Any(x => x.File_Name == fileName &&
                                                  x.ReportDate == reportDate &&
                                                  x.Version == version &&
                                                  x.TransferType == "Y")))
                {
                    return true;
                }
            }               
            return false;
        }

        /// <summary>
        /// 轉檔紀錄存到Sql(Transfer_CheckTable)
        /// </summary>
        /// <param name="fileName">檔案名稱 A41,A42...</param>
        /// <param name="flag">成功失敗</param>
        /// <param name="reportDate">基準日</param>
        /// <param name="version">版本</param>
        /// <param name="start">轉檔開始時間</param>
        /// <param name="end">轉檔結束時間</param>
        /// <returns></returns>
        public bool saveTransferCheck(
            string fileName,
            bool flag,
            DateTime reportDate,
            int version,
            DateTime start,
            DateTime end)
        {
            List<string> resetTable = new List<string>()
            {
                TableType.A53.ToString(),
                TableType.A84.ToString(),
                TableType.A07.ToString(),
                "C03",
                TableType.C04.ToString()
            };
            using (IFRS9Entities db = new IFRS9Entities())
            {
                if (flag && db.Transfer_CheckTable.AsNoTracking().Any(x =>
                               !resetTable.Contains(fileName) &&
                               x.ReportDate == reportDate &&
                               x.Version == version &&
                               x.File_Name == fileName &&
                               x.TransferType == "Y"))
                    return false;
                if (fileName == "C03" || EnumUtil.GetValues<TableType>()
                    .Select(x => x.ToString()).ToList().Contains(fileName))
                {
                    if (resetTable.Contains(fileName))
                    {
                        var updateTable = db.Transfer_CheckTable
                            .Where(x => x.File_Name == fileName &&
                                        x.ReportDate == reportDate &&
                                        x.TransferType == "Y").FirstOrDefault();
                        if (updateTable != null)
                            updateTable.TransferType = "R";
                    }
                    db.Transfer_CheckTable.Add(new Transfer_CheckTable()
                    {
                        File_Name = fileName,
                        ReportDate = reportDate,
                        Version = version,
                        TransferType = flag ? "Y" : "N",
                        Create_date = start.ToString("yyyyMMdd"),
                        Create_time = start.ToString("HH:mm:ss"),
                        End_date = end.ToString("yyyyMMdd"),
                        End_time = end.ToString("HH:mm:ss"),
                        Process = "Plan"
                    });
                    try
                    {
                        db.SaveChanges();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }
}