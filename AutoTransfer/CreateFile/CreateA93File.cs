using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA93File
    {
        public bool create(string dateTime)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(TableType.A93, dateTime);

                //ex: GetA93_20180110m.csv
                string getFileName = f.getA93FileName();

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add("PROGRAMFLAG=oneshot");
                data.Add("FILETYPE=pc");
                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("PROGRAMNAME=getdata");
                data.Add("CLOSINGVALUES=yes");
                data.Add("COLUMNHEADER=yes");
                data.Add("DELIMITER=,");
                data.Add("SECMASTER=yes");

                #endregion Title

                data.Add(string.Empty);

                #region START-OF-FIELDS
                data.Add("START-OF-FIELDS");
                data.Add("PX_LAST");
                data.Add("LAST_UPDATE_DT");
                data.Add("END-OF-FIELDS");
                #endregion START-OF-FIELDS

                data.Add(string.Empty);

                #region START-OF-DATA
                data.Add("START-OF-DATA");

                IFRS9Entities db = new IFRS9Entities();
                db.Gov_Info_Ticker.AsNoTracking()
                  .Where(x=>x.Foreign_Exchange_Map.ToString() != "")
                  .ToList().ForEach(x =>
                  {
                      data.Add(x.Foreign_Exchange_Map);
                  });
                db.Dispose();

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion START-OF-FIELDS

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putA93FilePath(),
                    f.putA93FileName(),
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