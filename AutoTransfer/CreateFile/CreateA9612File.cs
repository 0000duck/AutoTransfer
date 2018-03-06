using System;
using System.Collections.Generic;
using static AutoTransfer.Enum.Ref;
using System.Linq;
using AutoTransfer.Utility;

namespace AutoTransfer.CreateFile
{
    public class CreateA9612File
    {
        public bool create(TableType type, string dateTime, int ver)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(type, dateTime);

                //ex: GetA9612_20180131
                string getFileName = f.getA9612FileName();

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
                data.Add("BNCHMRK_TSY_ISSUE_ID");
                data.Add("END-OF-FIELDS");
                #endregion START-OF-FIELDS

                //空一行
                data.Add(string.Empty);

                #region START-OF-DATA
                data.Add("START-OF-DATA");
                int year = Int32.Parse(dateTime.Substring(0, 4));
                int month = Int32.Parse(dateTime.Substring(4, 2));
                int day = Int32.Parse(dateTime.Substring(6, 2));
                DateTime date = new DateTime(year, month, day);
                IFRS9Entities db = new IFRS9Entities();
                db.Bond_Account_Info.AsNoTracking()
                    .Where(x => x.Report_Date.HasValue &&
                                x.Report_Date.Value == date &&
                                x.Version.HasValue && x.Version == ver)
                    .Select(x => x.Bond_Number).Distinct()
                    .OrderBy(x => x)
                    .ToList().ForEach(x =>
                    {
                        if (!x.IsNullOrWhiteSpace())
                        {
                            data.Add(string.Format("{0} Corp|ISIN|", x));
                        }
                    });
                db.Dispose();
                data.Add("END-OF-DATA");
                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion File

                flag = new CreatePutFile().create(
                    f.putA9612FilePath(),
                    f.putA9612FileName(),
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