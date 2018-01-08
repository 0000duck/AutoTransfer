using System;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA94File
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

                SetFile f = new SetFile(TableType.A94, dateTime);

                //ex: GetA94_20180108.csv
                string getFileName = f.getFileName();

                DateTime dt = DateTime.Now;
                DateTime dt2 = dt.AddMonths(-18);

                #region File

                data.Add("START-OF-FILE");

                #region Title

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

                data.Add("NAME");
                data.Add("PX_LAST");
                data.Add("LAST_UPDATE_DT");

                data.Add("END-OF-FIELDS");

                #endregion START-OF-FIELDS

                //空一行
                data.Add(string.Empty);

                #region START-OF-DATA

                data.Add("START-OF-DATA");

                new Gov_Info_Ticker().GetType().GetProperties().ToList()
                    .ForEach(x =>
                                {
                                    data.Add(x.Name);
                                });

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putA94FilePath(),
                    f.putFileName(),
                    data);
            }
            catch
            {
                flag = false;
            }
            return flag;
        }
    }
}
