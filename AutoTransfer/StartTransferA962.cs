﻿using AutoTransfer.Transfer;
using System;

namespace AutoTransfer
{
    public class StartTransferA962
    {
        public void start()
        {
            DateTime LastDay = DateTime.Now.AddMonths(1).AddDays(-DateTime.Now.AddMonths(1).Day);
            new A962().startTransfer(LastDay.ToString("yyyyMMdd"));
        }
    }
}