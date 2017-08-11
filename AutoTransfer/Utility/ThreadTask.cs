using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTransfer.Utility
{
    public class ThreadTask
    {
        private int _interval = 20 * 60 * 1000; //20min

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

            Task.Factory.StartNew(() =>
            {
                while (this.IsRunning)
                {
                    SpinWait.SpinUntil(() => !this.IsRunning, this.Interval);
                    fun();
                }
            });
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
