﻿using AutoTransfer.Utility;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA9622File
    {
        public bool create(TableType type, string dateTime, List<Bond_Spread_Info> datas)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(type, dateTime);

                //ex: GetA9622_20180131
                string getFileName = f.getA9622FileName();

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("PROGRAMNAME=gethistory");
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add("SECID=ISIN");
                data.Add($"DATERANGE={dateTime}|{dateTime}");
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

                datas.Select(x => x.ID_CUSIP).Distinct()
                     .ToList()
                     .ForEach(x =>
                                 {
                                     if (!x.IsNullOrWhiteSpace())
                                     {
                                         data.Add(string.Format("{0}@BGN Govt|CUSIP|", x));
                                     }
                                 }
                             );

                data.Add("END-OF-DATA");
                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                flag = new CreatePutFile().create(
                       f.putA9622FilePath(),
                       f.putA9622FileName(),
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