using System.Collections.Generic;

namespace AutoTransfer.Abstract
{
    public abstract class SFTPTransfer
    {
        protected static List<string> nullarr = new List<string>() { "N.S.", "N.A." };

        protected class commpayInfo
        {
            public List<string> Bond_Number { get; set; }
            public string Rating_Object { get; set; }
        }

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime">排程日期(yyyyMMdd)</param>
        public abstract void startTransfer(string dateTime);

        /// <summary>
        /// create Sample req file
        /// </summary>
        protected abstract void createSampleFile();

        /// <summary>
        /// SFTP put sample 
        /// </summary>
        protected abstract void putSampleSFTP();

        /// <summary>
        /// SFTP get sample
        /// </summary>
        protected abstract void getSampleSFTP();

        /// <summary>
        /// create Commpany req file 
        /// </summary>
        protected abstract void createCommpanyFile();

        /// <summary>
        /// SFTP put Commpany 
        /// </summary>
        protected abstract void putCommpanySFTP();

        /// <summary>
        /// SFTP get Commpany 
        /// </summary>
        protected abstract void getCommpanySFTP();

        /// <summary>
        /// sample data & commpany data to db
        /// </summary>
        protected abstract void DataToDb();
    }
}
