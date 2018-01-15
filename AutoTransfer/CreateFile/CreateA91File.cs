using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA91File
    {
        public bool create(string dateTime, List<Gov_Info_Ticker> A94)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(TableType.A91, dateTime);

                //ex: GetA91_20180112y.csv
                string getFileName = f.getA91FileName();

                DateTime dt2 = DateTime.Now.AddYears(-1);

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FILETYPE=pc");
                data.Add($"REPLYFILENAME={getFileName}");
                data.Add($"DATERANGE={dt2.ToString("yyyy1231")}|{dateTime.Substring(0,4) + "1231"}");
                data.Add("HIST_PERIOD=y");
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

                A94.Select(x => x.IGS_Index_Map).Distinct()
                   .OrderBy(x => x)
                   .ToList().ForEach(x =>
                   {
                       if (!x.IsNullOrWhiteSpace())
                       {
                           data.Add(x);
                       }
                   });

                A94.Select(x => x.GDP_Yearly_Map).Distinct()
                   .OrderBy(x => x)
                   .ToList().ForEach(x =>
                   {
                       if (!x.IsNullOrWhiteSpace())
                       {
                           data.Add(x);
                       }
                   });

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion START-OF-FIELDS

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putA91FilePath(),
                    f.putA91FileName(),
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