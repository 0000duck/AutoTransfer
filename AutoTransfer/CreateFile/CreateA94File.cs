using System;
using System.Collections.Generic;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA94File
    {
        //create Put A94 File
        public bool create(string dateTime, string yqm)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(TableType.A94, dateTime);

                //ex: GetA94_20180108y.csv
                //ex: GetA94_20180108q.csv
                //ex: GetA94_20180108m.csv
                string getFileName = f.getA94FileName(yqm);

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add("FIRMNAME=" + f.getFIRMNAME());
                if (yqm == "q" || yqm == "m")
                {
                    data.Add("FILETYPE=oneshot");
                }
                data.Add("FILETYPE=pc");
                data.Add($"REPLYFILENAME={getFileName}");
                if (yqm == "m")
                {
                    data.Add("PROGRAMNAME=getdata");
                    data.Add("CLOSINGVALUES=yes");
                    data.Add("COLUMNHEADER=yes");
                    data.Add("DELIMITER=,");
                    data.Add("SECMASTER=yes");
                }
                if (yqm == "y" || yqm == "q")
                {
                    data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                    data.Add($"DATERANGE={dateTime}|{dateTime}");
                    data.Add($"HIST_PERIOD={yqm}");
                    data.Add("PROGRAMNAME=gethistory");
                }

                #endregion Title

                data.Add(string.Empty);

                #region START-OF-FIELDS
                data.Add("START-OF-FIELDS");
                data.Add("PX_LAST");
                if (yqm == "m")
                {
                    data.Add("LAST_UPDATE_DT");
                }
                data.Add("END-OF-FIELDS");
                #endregion START-OF-FIELDS

                data.Add(string.Empty);

                #region START-OF-DATA
                data.Add("START-OF-DATA");

                switch (yqm)
                {
                    case "y":
                        data.Add("IGS%TUR Index");
                        data.Add("IGS%BRA Index");
                        data.Add("IGS%ISR Index");
                        data.Add("IGS%QAT Index");
                        data.Add("IGS%IND Index");
                        data.Add("IGS%SAU Index");
                        data.Add("IGS%RUS Index");
                        data.Add("IGS%ZAF Index");
                        data.Add("IGS%MEX Index");
                        data.Add("IGS%CHL Index");
                        data.Add("IGS%PHL Index");
                        data.Add("IGS%USA Index");
                        data.Add("EHGDTRY Index");
                        data.Add("EHGDBRY Index");
                        data.Add("EHGDILY Index");
                        data.Add("EHGDQAY Index");
                        data.Add("EHGDIDY Index");
                        data.Add("SRGDPCYY Index");
                        data.Add("EHGDRUY Index");
                        data.Add("EHGDZAY Index");
                        data.Add("EHGDMXY Index");
                        data.Add("EHGDCLY Index");
                        data.Add("EHGDPHY Index");
                        data.Add("EHGDUSY Index");
                        break;
                    case "q":
                        data.Add("HELDTRDS Index");
                        data.Add("HELDBRDS Index");
                        data.Add("HELDILDS Index");
                        data.Add("HELDIDDS Index");
                        data.Add("HELDRUDS Index");
                        data.Add("HELDZA59 Index");
                        data.Add("HELDMXS Index");
                        data.Add("HELDCLS Index");
                        data.Add("HELDPHS Index");
                        data.Add("HELDUSDS Index");
                        break;
                    case "m":
                        data.Add("TUIRCBFX Index");
                        data.Add("BZIDFCUR Index");
                        data.Add("ISFCBAL Index");
                        data.Add("453.055 Index");
                        data.Add("IDGFA Index");
                        data.Add("456.055 Index");
                        data.Add("RUFGGFML Index");
                        data.Add("SANOGR$ Index");
                        data.Add("MXIRINUS Index");
                        data.Add("CHMRRSRV Index");
                        data.Add("PHIRTTL Index");
                        data.Add("WIRAUS Index");
                        break;
                }

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion START-OF-FIELDS

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putA94FilePath(),
                    f.putA94FileName(yqm),
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