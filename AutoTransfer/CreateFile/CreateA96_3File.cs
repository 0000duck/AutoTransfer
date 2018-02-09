﻿using System.Collections.Generic;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA96_3File
    {
        public bool create(TableType type, string dateRageStart, string dateRageEnd, string dateTime, List<string> datas)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(type, dateTime);

                //ex: GetA96_3_20180131
                string getFileName = f.getA96_3FileName();

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("PROGRAMNAME=gethistory");
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add("SECID=ISIN");
                data.Add($"DATERANGE={dateRageStart}|{dateRageEnd}");
                data.Add("HIST_PERIOD=d");

                #endregion Title

                //空一行
                data.Add(string.Empty);

                #region START-OF-FIELDS
                data.Add("START-OF-FIELDS");
                data.Add("YLD_YTM_MID");
                data.Add("END-OF-FIELDS");
                #endregion START-OF-FIELDS

                //空一行
                data.Add(string.Empty);

                #region START-OF-DATA
                data.Add("START-OF-DATA");
                datas.ForEach(x => data.Add(string.Format("{0}@BGN Govt|CUSIP|", x)));
                data.Add("END-OF-DATA");
                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                flag = new CreatePutFile().create(
                       f.putA96_3FilePath(),
                       f.putA96_3FileName(),
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