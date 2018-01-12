using System;
using System.Collections.Generic;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA92File
    {
        public bool create(string dateTime, List<Gov_Info_Ticker> A94)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(TableType.A92, dateTime);

                //ex: GetA92_20180112q.csv
                string getFileName = f.getA92FileName();

                DateTime dt2 = DateTime.Now.AddYears(-1);

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FILETYPE=pc");
                data.Add($"REPLYFILENAME={getFileName}");
                data.Add($"DATERANGE={dt2.ToString("yyyy0331")}|{dateTime}");
                data.Add("HIST_PERIOD=q");
                data.Add("PROGRAMNAME=gethistory");

                #endregion Title

                data.Add(string.Empty);

                #region START-OF-FIELDS
                data.Add("START-OF-FIELDS");
                data.Add("PX_LAST");
                data.Add("END-OF-FIELDS");
                #endregion START-OF-FIELDS

                data.Add(string.Empty);

                #region START-OF-DATA
                data.Add("START-OF-DATA");

                A94.ForEach(x =>
                {
                    data.Add(x.Short_term_Debt_Map);
                });

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion START-OF-FIELDS

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putA92FilePath(),
                    f.putA92FileName(),
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