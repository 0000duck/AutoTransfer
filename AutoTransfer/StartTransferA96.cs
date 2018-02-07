using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA96
    {
        public void start()
        {
            //DateTime LastDay = DateTime.Now.AddMonths(1).AddDays(-DateTime.Now.AddMonths(1).Day);
            //new A96().startTransfer(LastDay.ToString("yyyyMMdd"));

            new A96().startTransfer("20170531");
        }
    }
}