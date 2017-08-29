using AutoTransfer.Utility;
using System;

namespace AutoTransfer
{
    public class StartTransfer
    {
        public void start(string dateTime = null)
        {
            if (dateTime.IsNullOrWhiteSpace())
                dateTime = DateTime.Now.ToString("yyyyMMdd");
            new FileRelated().createFile(@"D:\fubon\testSuccess");
        }
    }
}