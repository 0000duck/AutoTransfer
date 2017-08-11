using System;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class CompleteEvent
    {
        private IFRS9Entities db = new IFRS9Entities();

        /// <summary>
        /// 每一個自動轉檔成功後會觸發的程式
        /// </summary>
        /// <param name="reportDate"></param>
        public void trigger(DateTime reportDate)
        {
            List<string> completeType = new List<string>(); //需成功的轉檔類型
            List<string> otherType = new List<string>() //其他的(須排除)檔案類性
            {
                TableType.A57.ToString(),
                TableType.A58.ToString(),
                TableType.A60.ToString()
            }; 

            foreach (TableType item in System.Enum.GetValues(typeof(TableType)))
            {
                if (!otherType.Contains(item.ToString()))
                    completeType.Add(item.ToString());
            }
            if (db.IFRS9_SFTP_Log.Select(x =>
                   x.Report_Date == reportDate &&
                   x.Flag == true &&
                   completeType.Contains(x.Transfer_Type)                                    
                  ).Count().Equals(completeType.Count)) //全部成功(所有轉檔都完成)後接下去執行
            {
                saveDb(reportDate);
            }
            else
            {
                db.Dispose();
            }
        }

        private void saveDb(DateTime reportDate)
        {
            //List<string> Bond_Numbers = new List<string>();
            //Bond_Numbers.AddRange(db.Rating_SP_Info //A53
            //    .Where(x =>  reportDate == x.Rating_Date)
            //    .Select(x => x.Bond_Number).Distinct());
            //Bond_Numbers.AddRange(db.Rating_Moody_Info //A54
            //    .Where(x => reportDate == x.Rating_Date)
            //    .Select(x => x.Bond_Number).Distinct());
            //Bond_Numbers.AddRange(db.Rating_Fitch_Info
            //    .Where(x=> reportDate == x.r)
            //    )
            //db.Bond_Account_Info.Select()
        }  
    }
}
