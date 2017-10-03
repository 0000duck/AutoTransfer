using AutoTransfer.Commpany;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateCommpanyFile
    {
        public bool create(TableType type, string dateTime, List<string> datas)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(type, dateTime);

                //ex: commpanyA53_20170803
                string getFileName = f.getCommpanyFileName();

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("PROGRAMNAME=getcompany");
                data.Add("PROGRAMFLAG="+f.getPROGRAMFLAG());
                data.Add("FIRMNAME="+f.getFIRMNAME()); //確認是否提出來?
                data.Add("CREDITRISK=yes");
                data.Add("SECID=TICKER");

                #endregion Title

                //空一行
                data.Add(string.Empty);

                #region START-OF-FIELDS

                data.Add("START-OF-FIELDS");
                object obj = null;
                bool findFlag = false;
                if (TableType.A53.ToString().Equals(type.ToString()))
                {
                    obj = new A53Commpany();
                    findFlag = true;
                }
                if (findFlag)
                    obj.GetType()
                       .GetProperties()
                       //.OrderBy(x => x.Name)
                       .ToList()
                       .ForEach(x => data.Add(x.Name));
                data.Add("END-OF-FIELDS");

                #endregion START-OF-FIELDS

                //空一行
                data.Add(string.Empty);

                #region START-OF-DATA

                data.Add("START-OF-DATA");
                datas.ForEach(x => data.Add(string.Format("{0} Equity|TICKER", x)));
                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                //ex: ../commpanyPut 資料夾
                //f.putCommpanyFilePath();
                //ex: commpanyA53_20170803
                //f.putCommpanyFileName();
                flag = new CreatePutFile().create(
                    f.putCommpanyFilePath(),
                    f.putCommpanyFileName(),
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