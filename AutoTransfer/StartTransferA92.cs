using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA92
    {
        public void start()
        {
            new A92().startTransfer(DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}
