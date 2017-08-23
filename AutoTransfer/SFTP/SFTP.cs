using Renci.SshNet;
using System;
using System.IO;

namespace AutoTransfer.SFTPConnect
{
    public class SFTP
    {
        private string m_SFTPAccount;
        private SftpClient m_SftpClient;
        private string m_SFTPHost;
        private string m_SFTPPassword;

        public SFTP(string host, string account, string password)
        {
            this.m_SFTPHost = host;
            this.m_SFTPAccount = account;
            this.m_SFTPPassword = password;

            this.m_SftpClient = new SftpClient(this.m_SFTPHost, 22, this.m_SFTPAccount, this.m_SFTPPassword);
        }

        #region 屬性

        public bool IsConnect
        {
            get
            {
                if (this.m_SftpClient != null)
                {
                    return this.m_SftpClient.IsConnected;
                }
                else
                {
                    return false;
                }
            }
        }

        public SftpClient SftpClient
        {
            get
            {
                return this.m_SftpClient;
            }
        }

        #endregion 屬性

        /// <summary>
        /// 從SFTPServer下載檔案
        /// </summary>
        /// <param name="SFTPDirectory"></param>
        /// <param name="localDirectory"></param>
        /// <param name="fileName"></param>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        public bool Get(string SFTPDirectory, string localDirectory, string fileName, out string errorInfo)
        {
            try
            {
                this.connect(out errorInfo);

                #region Error Check

                if (errorInfo != null)
                {
                    throw new Exception(errorInfo);
                }

                #endregion Error Check

                try
                {
                    MemoryStream _CheckFileStream = new MemoryStream();
                    this.m_SftpClient.DownloadFile(Path.Combine(SFTPDirectory, fileName), _CheckFileStream);    //由SFTP抓到檔案

                    //long _ReportFileSize = 0;
                    using (FileStream _Stream = File.OpenWrite(Path.Combine(localDirectory, fileName)))         //於本機端開啟一空白檔案
                    {
                        try
                        {
                            _CheckFileStream.Seek(0, SeekOrigin.Begin);
                            _CheckFileStream.CopyTo(_Stream);
                            //using (BinaryWriter _SW = new BinaryWriter(_Stream))
                            //{
                            //    using (MemoryStream _SourceStream = _CheckFileStream)                           //SFTP抓到檔案的串流
                            //    {
                            //        int _BufferLength = 4096;
                            //        byte[] _Buffer = new byte[_BufferLength];

                            //        int _ContentLen = _SourceStream.Read(_Buffer, 0, _BufferLength);

                            //        while (_ContentLen != 0)
                            //        {
                            //            _ReportFileSize += _ContentLen;

                            //            _SW.Write(_Buffer, 0, _ContentLen);                                     //分批寫到本機端上

                            //            _ContentLen = _SourceStream.Read(_Buffer, 0, _BufferLength);
                            //        }

                            //        _SourceStream.Close();
                            //    }
                            //}
                        }
                        catch (NotSupportedException ex)
                        {
                            errorInfo = ex.Message;
                            return false;
                        }
                        catch (Exception ex)
                        {
                            errorInfo = ex.Message;
                            return false;
                        }
                        finally
                        {
                            _CheckFileStream.Close();
                            _Stream.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorInfo = ex.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorInfo = ex.Message;
                return false;
            }
            finally
            {
                this.m_SftpClient.Disconnect();
            }
            return true;
        }

        public bool Put(string filePath, string replyFileName, out string errorInfo)
        {
            return this.Put(string.Empty, filePath, replyFileName, out errorInfo);
        }

        /// <summary>
        /// 檔案上傳SFTPServer
        /// </summary>
        /// <param name="SFTPDirectory"></param>
        /// <param name="filePath"></param>
        /// <param name="replyFileName"></param>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        public bool Put(string SFTPDirectory, string filePath, string replyFileName, out string errorInfo)
        {
            errorInfo = null;
            try
            {
                this.connect(out errorInfo);

                #region Error Check

                if (errorInfo != null)
                {
                    throw new Exception(errorInfo);
                }

                #endregion Error Check

                //目前預設傳給Bloomberg Data License req檔
                string _ExistFile = Path.Combine(filePath, replyFileName);
                string _SFTPPath = string.IsNullOrEmpty(SFTPDirectory) ? replyFileName : Path.Combine(SFTPDirectory, replyFileName);
                if (!File.Exists(_ExistFile))
                {
                    errorInfo = "查無此檔案";
                    return false;
                }

                long _ReportFileSize = 0;
                using (Stream _Stream = this.m_SftpClient.OpenWrite(_SFTPPath)) //在SFTP Server上開啟一空白檔案
                {
                    try
                    {
                        using (BinaryWriter _SW = new BinaryWriter(_Stream))
                        {
                            using (FileStream _SourceStream = System.IO.File.Open(_ExistFile, FileMode.Open, FileAccess.Read, FileShare.None))//讀取上傳檔案的串流
                            {
                                int _BufferLength = 4096;
                                byte[] _Buffer = new byte[_BufferLength];

                                int _ContentLen = _SourceStream.Read(_Buffer, 0, _BufferLength);

                                while (_ContentLen != 0)
                                {
                                    _ReportFileSize += _ContentLen;

                                    _SW.Write(_Buffer, 0, _ContentLen);//將上傳檔案的串流，分批寫入到SFTP Server上建立的檔案中

                                    _ContentLen = _SourceStream.Read(_Buffer, 0, _BufferLength);
                                }

                                _SourceStream.Close();
                            }
                        }
                    }
                    finally
                    {
                        _Stream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                errorInfo = ex.Message;
                return false;
            }
            finally
            {
                this.m_SftpClient.Disconnect();
            }

            return true;
        }

        private bool connect(out string errorInfo)
        {
            errorInfo = null;
            try
            {
                if (this.m_SftpClient != null && !IsConnect)
                {
                    this.m_SftpClient.Connect();
                }
                return true;
            }
            catch (Exception ex)
            {
                errorInfo = "SFTP connect Error: " + ex.Message;
                return false;
            }
        }
    }
}