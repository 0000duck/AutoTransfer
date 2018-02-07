using System;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA07File
    {
        /// <summary>
        /// create Put A07 File
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dateTime"></param>
        public bool create(TableType type, string dateTime)
        {
            bool flag = false;

            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(type, dateTime);

                //ex: GetA07_20170908.csv
                string getFileName = f.getFileName();
                //string getFileName = f.getGZFileName();

                DateTime dt = DateTime.Now;
                //DateTime dt2 = dt.AddMonths(-18);

                #region File
                data.Add("START-OF-FILE");

                #region Title
                data.Add("FIRMNAME="+ f.getFIRMNAME());
                data.Add("PROGRAMFLAG="+f.getPROGRAMFLAG());
                data.Add("FILETYPE=pc");
                data.Add($"REPLYFILENAME={getFileName}");
                data.Add($"DATERANGE=19950331|{dt.ToString("yyyy1231")}");
                data.Add("HIST_PERIOD=q");
                data.Add("PROGRAMNAME=gethistory");
                #endregion Title

                //空一行
                data.Add(string.Empty);

                #region START-OF-FIELDS
                data.Add("START-OF-FIELDS");
                data.Add("PX_LAST");
                data.Add("END-OF-FIELDS");
                #endregion START-OF-FIELDS

                //空一行
                data.Add(string.Empty);

                #region START-OF-DATA
                data.Add("START-OF-DATA");

                new Econ_Domestic().GetType().GetProperties()
                    .Skip(2).ToList().ForEach(x =>
                    {
                        if (x.Name == "SINYI_HOUSE_PRICE_index" || x.Name == "CATHAY_ESTATE_index" || x.Name == "Real_GDP2011")
                        {
                            data.Add(x.Name);
                        }
                        else if(x.Name != "Econ_D_YYYYMMDD")
                        {
                            data.Add(x.Name.Replace("_", " "));
                        }
                    });

                data.Add("END-OF-DATA");
                #endregion START-OF-DATA

                data.Add("END-OF-FILE");
                #endregion File

                flag = new CreatePutFile().create(
                    f.putA07FilePath(),
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