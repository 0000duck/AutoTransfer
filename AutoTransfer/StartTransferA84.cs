using AutoTransfer.Transfer;
using AutoTransfer.Utility;
using System;

namespace AutoTransfer
{
    public class StartTransferA84
    {
        public void start()
        {
            //DateTime LastDay = DateTime.Now.AddMonths(1).AddDays(-DateTime.Now.AddMonths(1).Day);
            //new A84().startTransfer(LastDay.ToString("yyyyMMdd"));
            new A84().startTransfer(DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}