using AutoTransfer.Transfer;
using AutoTransfer.Utility;
using System;

namespace AutoTransfer
{
    public class StartTransferC04
    {
        public void start()
        {
            DateTime dt = DateTime.Now;
            new C04().startTransfer(dt.Year.ToString());
            //new C04().startTransfer((dt.Year - 1).ToString());
        }
    }
}