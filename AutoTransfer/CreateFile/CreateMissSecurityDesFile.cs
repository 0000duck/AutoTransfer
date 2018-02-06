using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateMissSecurityDesFile
    {
        public bool Create(TableType type, string dateTime, List<string> datas)
        {
            bool flag = false;
            try
            {
                SetFile f = new SetFile(type, dateTime);

                List<string> data = new List<string>();

                //ex: securityDes_20180131.csv
                string getFileName = f.getSecurityDesFileName();

                #region File

                data.Add("START-OF-FILE");

                #region Title
                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("PROGRAMNAME=getdata");
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FIRMNAME=" + f.getFIRMNAME()); //確認是否提出來?
                data.Add("SECMASTER=YES");
                data.Add("OUTPUTFORMAT=bulklist");
                data.Add("DELIMITER=,");
                data.Add("FUNDAMENTALS=yes");
                #endregion Title

                //空一行
                data.Add(string.Empty);

                #region START-OF-FIELDS

                data.Add("START-OF-FIELDS");

                data.Add("Security_Des");

                data.Add("END-OF-FIELDS");

                #endregion START-OF-FIELDS

                data.Add(string.Empty);

                #region START-OF-DATA

                data.Add("START-OF-DATA");

                datas.ForEach(x =>
                {
                    data.Add(string.Format("{0} Corp", x));
                });

                data.Add("END-OF-DATA");

                #endregion

                data.Add("END-OF-FILE");

                #endregion File

                flag = new CreatePutFile().create(
                        f.putSecurityDesFilePath(),
                        f.putSecurityDesFileName(),
                        data);
            }
            catch 
            {
            }
            return flag;
        }
    }
}
