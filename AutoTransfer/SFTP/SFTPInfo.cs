namespace AutoTransfer.SFTPConnect
{
    public static class SFTPInfo
    {
        private static string _account =
            "dl221";

        private static string _ip =
                    "sftp.bloomberg.com";

        private static string _password =
            "whz1+Vvs.cduo=eY";

        public static string account
        {
            get { return _account; }
            set { _account = value; }
        }

        public static string ip
        {
            get { return _ip; }
            set { _ip = value; }
        }

        public static string password
        {
            get { return _password; }
            set { _password = value; }
        }
    }
}