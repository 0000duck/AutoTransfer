using AutoTransfer.Sample;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateGetC04File
    {
        /// <summary>
        /// create Put Sample File
        /// </summary>
        /// <param name="dateTime"></param>
        public bool create(string dateTime)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(TableType.C04, dateTime);

                //ex: GetC04_20170803_1.csv
                string getFileName = f.getFileName("1");

                DateTime dt = DateTime.Now;
                DateTime dt2 = dt.AddMonths(-18);

                #region 檔案1 排除(NEUENTTR Index,USDR1T Curncy) 抓月的 要加 HIST_PERIOD & 三個參數(USERNUMBER,WS,SN)
                #region File

                data.Add("START-OF-FILE");

                #region Title
                data.Add("USERNUMBER=" + f.getUSERNUMBER());
                data.Add("WS=" + f.getWS());
                data.Add("SN=" + f.getSN());
                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("FILETYPE=pc");
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add($"DATERANGE={dt2.ToString("yyyyMMdd")}|{dt.ToString("yyyyMMdd")}");
                data.Add("HIST_PERIOD=q");
                data.Add("PROGRAMNAME=gethistory");

                #endregion Title

                //空一行
                data.Add(string.Empty);

                #region START-OF-FIELDS

                data.Add("START-OF-FIELDS");

                //data.Add("NAME");
                data.Add("PX_LAST");
                //data.Add("LAST_UPDATE_DT");

                data.Add("END-OF-FIELDS");

                #endregion START-OF-FIELDS

                //空一行
                data.Add(string.Empty);

                #region START-OF-DATA

                data.Add("START-OF-DATA");

                new Econ_Foreign().GetType().GetProperties()
                    .Skip(2).ToList().ForEach(x =>
                    {
                        if (x.Name == "CNFRBAL_Index") //貿易收支 傳送要加$
                        {
                            data.Add(x.Name.Replace("_", "$ "));
                        }
                        else if (x.Name != "NEUENTTR_Index" && x.Name != "USDR1T_Curncy")
                        {
                            data.Add(x.Name.Replace("_", " "));
                        }
                    });

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putC04FilePath(),
                    f.putFileName("1"),
                    data);

                if (!flag)
                    return flag;
                #endregion

                #region 檔案2 (NEUENTTR Index) 抓季的 不要加 HIST_PERIOD
                List<string> data2 = new List<string>();

                //ex: GetC04_20170803_2.csv
                string _getFileName2 = f.getFileName("2");

                #region File (NEUENTTR Index)

                data2.Add("START-OF-FILE");

                #region Title
                data2.Add("USERNUMBER=" + f.getUSERNUMBER());
                data2.Add("WS=" + f.getWS());
                data2.Add("SN=" + f.getSN());
                data2.Add($"REPLYFILENAME={_getFileName2}");
                data2.Add("FILETYPE=pc");
                data2.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data2.Add("FIRMNAME=" + f.getFIRMNAME());
                data2.Add($"DATERANGE={dt2.ToString("yyyyMMdd")}|{dt.ToString("yyyyMMdd")}");
                //data2.Add("HIST_PERIOD=q"); //,所以req檔發動時要把HIST_PERIOD=q 拿掉,
                //但是這支抓到的資料每月都會有,所以需要判斷寫入DB時只寫3,6,9,12的資料,才是季的資料。
                data2.Add("PROGRAMNAME=gethistory");

                #endregion Title

                //空一行
                data2.Add(string.Empty);

                #region START-OF-FIELDS

                data2.Add("START-OF-FIELDS");

                data2.Add("PX_LAST");

                data2.Add("END-OF-FIELDS");

                #endregion START-OF-FIELDS

                //空一行
                data2.Add(string.Empty);

                #region START-OF-DATA

                data2.Add("START-OF-DATA");

                data2.Add("NEUENTTR Index");

                data2.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data2.Add("END-OF-FILE");

                #endregion File

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putC04FilePath(),
                    f.putFileName("2"),
                    data2);
                if (!flag)
                    return flag;
                #endregion

                #region 檔案3 (USDR1T Curncy)  不要加 三個參數(USERNUMBER,WS,SN)
                List<string> data3 = new List<string>();

                //ex: GetC04_20170803_3.csv
                string _getFileName3 = f.getFileName("3");

                #region File (USDR1T Curncy)

                data3.Add("START-OF-FILE");

                #region Title
                data3.Add($"REPLYFILENAME={_getFileName3}");
                data3.Add("FILETYPE=pc");
                data3.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data3.Add("FIRMNAME=" + f.getFIRMNAME());
                data3.Add($"DATERANGE={dt2.ToString("yyyyMMdd")}|{dt.ToString("yyyyMMdd")}");
                data3.Add("HIST_PERIOD=q");
                data3.Add("PROGRAMNAME=gethistory");

                #endregion Title

                //空一行
                data3.Add(string.Empty);

                #region START-OF-FIELDS

                data3.Add("START-OF-FIELDS");

                data3.Add("PX_LAST");

                data3.Add("END-OF-FIELDS");

                #endregion START-OF-FIELDS

                //空一行
                data3.Add(string.Empty);

                #region START-OF-DATA

                data3.Add("START-OF-DATA");

                data3.Add("USDR1T Curncy");

                data3.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data3.Add("END-OF-FILE");

                #endregion File

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putC04FilePath(),
                    f.putFileName("3"),
                    data3);
                #endregion
            }
            catch
            {
                flag = false;
            }
            return flag;
        }
    }
}