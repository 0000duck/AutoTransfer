using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA962
    {
        public void start()
        {
            DateTime LastDay = DateTime.Now.AddDays(-DateTime.Now.Day); //上個月最後一天
            new A962().startTransfer(LastDay.ToString("yyyyMMdd"));
        }
    }
}