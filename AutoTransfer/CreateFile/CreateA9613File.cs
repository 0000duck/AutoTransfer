using System.Collections.Generic;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA9613File
    {
        public bool create(TableType type, string dateTime, List<string> datas)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(type, dateTime);

                //ex: GetA9613_20180131
                string getFileName = f.getA9613FileName();

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("PROGRAMNAME=getdata");
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add("SECMASTER=YES");
                data.Add("OUTPUTFORMAT=bulklist");
                data.Add("DELIMITER=,");
                data.Add("FUNDAMENTALS=yes");

                #endregion Title

                //空一行
                data.Add(string.Empty);

                #region START-OF-FIELDS
                data.Add("START-OF-FIELDS");
                data.Add("ID_CUSIP");
                data.Add("END-OF-FIELDS");
                #endregion START-OF-FIELDS

                //空一行
                data.Add(string.Empty);

                #region START-OF-DATA
                data.Add("START-OF-DATA");
                datas.ForEach(x => data.Add(string.Format("{0}|CUSIP|", x)));
                data.Add("END-OF-DATA");
                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                flag = new CreatePutFile().create(
                       f.putA9613FilePath(),
                       f.putA9613FileName(),
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