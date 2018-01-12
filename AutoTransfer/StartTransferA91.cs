using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA91
    {
        public void start()
        {
            new A91().startTransfer(DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}