using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA07
    {
        public void start()
        {
            new A07().startTransfer(DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}