using System;
using System.Collections.Generic;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.CreateFile
{
    public class CreateA94File
    {
        /// <summary>
        /// create Put A94 File
        /// </summary>
        /// <param name="dateTime"></param>
        public bool create(string dateTime)
        {
            bool flag = false;
            try
            {
                List<string> data = new List<string>();

                SetFile f = new SetFile(TableType.A94, dateTime);

                //ex: GetA94_20180108.csv
                string getFileName = f.getFileName();

                DateTime dt = DateTime.Now;
                DateTime dt2 = dt.AddMonths(-18);

                #region File

                data.Add("START-OF-FILE");

                #region Title

                data.Add($"REPLYFILENAME={getFileName}");
                data.Add("FILETYPE=pc");
                data.Add("PROGRAMFLAG=" + f.getPROGRAMFLAG());
                data.Add("FIRMNAME=" + f.getFIRMNAME());
                data.Add($"DATERANGE={dt2.ToString("yyyyMMdd")}|{dt.ToString("yyyyMMdd")}");
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

                data.Add("END-OF-DATA");

                #endregion START-OF-DATA

                data.Add("END-OF-FILE");

                #endregion START-OF-FIELDS

                //建立 req 檔案
                flag = new CreatePutFile().create(
                    f.putA94FilePath(),
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
