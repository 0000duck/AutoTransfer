using AutoTransfer.Transfer;
using AutoTransfer.Utility;
using System;

namespace AutoTransfer
{
    public class StartTransfer
    {
        public void start()
        {
            DateTime LastDay = DateTime.Now.AddMonths(1).AddDays(-DateTime.Now.AddMonths(1).Day);
            new A53().startTransfer(LastDay.ToString("yyyyMMdd"));
        }
    }
}