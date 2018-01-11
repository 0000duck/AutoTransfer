using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA93
    {
        public void start()
        {
            new A93().startTransfer(DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}
