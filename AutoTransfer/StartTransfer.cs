using AutoTransfer.Transfer;
using AutoTransfer.Utility;
using System;

namespace AutoTransfer
{
    public class StartTransfer
    {
        public void start(string dateTime = null)
        {
            if(dateTime.IsNullOrWhiteSpace())
            dateTime = DateTime.Now.ToString("yyyyMMdd");
            //new A53().startTransfer(dateTime);
            //new A54().startTransfer(dateTime);
            //new A55().startTransfer(dateTime);
            //new A56().startTransfer(dateTime);
            new FileRelated().createFile(@"D:\fubon\testSuccess");
        }
    }
}
