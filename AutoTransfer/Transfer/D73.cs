using AutoTransfer.Utility;
using System.IO;

namespace AutoTransfer.Transfer
{
    public class D73
    {
        public void AddD73(Scheduling_Report D73, string sb)
        {
            try
            {
                using (IFRS9Entities db = new IFRS9Entities())
                {
                    string txtPath = @"D:\IFRS9CheckTable";
                    string configFileName = System.Configuration.ConfigurationManager.AppSettings["checkTable"];
                    if (!string.IsNullOrWhiteSpace(configFileName))
                        txtPath = configFileName; //config 設定就取代
                    new FileRelated().createFile(txtPath);
                    var _FilePath = Path.Combine(txtPath, D73.File_Name);
                    D73.File_path = _FilePath;
                    File.WriteAllText(_FilePath, sb);
                    db.Scheduling_Report.Add(D73);
                    db.SaveChanges();
                }
            }
            catch
            {
            }
        }
    }
}
