﻿using AutoTransfer.Sample;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateGetC04File
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

                SetFile f = new SetFile(TableType.C04, dateTime);

                //ex: GetC04_20170803.csv
                string getFileName = f.getFileName();

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("FILETYPE=pc");
                data.Add("PROGRAMFLAG=oneshot");
                data.Add("FIRMNAME=dl221"); //確認是否提出來?
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

                new Econ_Foreign().GetType().GetProperties()
                    .Skip(2).ToList().ForEach(x =>
                    {
                        data.Add(x.Name.Replace("_", " "));
                    });

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putC04FilePath(),
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