using AutoTransfer.Transfer;
using AutoTransfer.Utility;
using System;

namespace AutoTransfer
{
    public class StartTransferC04
    {
        public void start()
        {
            new C04().startTransfer(DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}