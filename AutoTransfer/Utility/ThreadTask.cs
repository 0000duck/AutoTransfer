using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTransfer.Utility
{
    public class ThreadTask
    {
        private int _interval = 20 * 60 * 1000; //20min
        private Task t = null;

        public int Interval
        {
            get { return _interval; }
            set { _interval = value; }
        }

        public bool IsRunning { get; internal set; }

        public void Start(Action fun)
        {
            if (this.IsRunning)
            {
                return;
            }

            this.IsRunning = true;

            t = new Task(() =>
            {
                while (this.IsRunning)
                {
                    SpinWait.SpinUntil(() => !this.IsRunning, this.Interval);
                    fun();
                    try
                    {
                        t.Wait();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    t.Dispose();
                }
            });
            t.Start();


            //Task.Factory.StartNew(() =>
            //{
            //    while (this.IsRunning)
            //    {
            //        SpinWait.SpinUntil(() => !this.IsRunning, this.Interval);
            //        fun();
            //    }
            //})
            //.ContinueWith(task =>
            //{
            //    fun();
            //})
        }

        public void Stop()
        {
            if (!this.IsRunning)
            {
                return;
            }
            this.IsRunning = false;
        }
    }
}