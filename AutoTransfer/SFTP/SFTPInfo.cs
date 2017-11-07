namespace AutoTransfer.SFTPConnect
{
    public static class SFTPInfo
    {
        private static string _account = "dl789940";

        private static string _ip = "sftp.bloomberg.com";

        private static string _password = "S4GkU9,Znmjz[d7y";

        public static string account
        {
            get { return _account; }
            private set { _account = value; }
        }

        public static string ip
        {
            get { return _ip; }
            private set { _ip = value; }
        }

        public static string password
        {
            get { return _password; }
            private set { _password = value; }
        }
    }
}