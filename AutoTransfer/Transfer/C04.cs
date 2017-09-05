using AutoTransfer.Abstract;
using AutoTransfer.Commpany;
using AutoTransfer.CreateFile;
using AutoTransfer.Sample;
using AutoTransfer.SFTPConnect;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Threading;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class C04
    {
        #region 共用參數

        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private int verInt = 0;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.C04;
        private string type = TableType.C04.ToString();

        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public void startTransfer(string dateTime)
        {
            createGetC04File();
        }

        /// <summary>
        /// 建立 GetC04 要Put的檔案
        /// </summary>
        private void createGetC04File()
        {
            //建立  檔案
            // create ex:GetC04_20170807
            if (new CreateGetC04File().create(tableType, reportDateStr))
            {
                //把資料送給SFTP
                putGetC04SFTP();
            }
            else
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Create_File_Fail.GetDescription(type));
            }
        }

        /// <summary>
        /// SFTP Put Sample檔案
        /// </summary>
        private void putGetC04SFTP()
        {
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putSampleFilePath(),
                 setFile.putSampleFileName(),
                 out error);
            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Put_File_Fail.GetDescription(type));
            }
            else //success (wait 20 min and get data)
            {
                Thread.Sleep(20 * 60 * 1000);
                getGetC04SFTP();
            }
        }

        /// <summary>
        /// SFTP GetC04 檔案
        /// </summary>
        private void getGetC04SFTP()
        {
            new FileRelated().createFile(setFile.getC04FilePath());
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Get(string.Empty,
                setFile.getSampleFilePath(),
                setFile.getSampleFileName(),
                out error);
            if (!error.IsNullOrWhiteSpace())
            {
                log.txtLog(
                    type,
                    false,
                    startTime,
                    logPath,
                    MessageType.Get_File_Fail.GetDescription(type));
            }
            else
            {
            }
        }
    }
}