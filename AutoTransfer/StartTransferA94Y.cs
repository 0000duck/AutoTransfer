using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA94y
    {
        public void start()
        {
            new A94().startTransfer(DateTime.Now.ToString("yyyyMMdd"),"y");
        }
    }
}
