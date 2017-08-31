using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferC03Mortgage
    {
        public void start()
        {
            new C03Mortgage().startTransfer(DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}