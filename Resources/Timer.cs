using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoFollow.Resources
{
    public class Timer
    {
        public Stopwatch StopWatch { get; set; }
        private bool _hasExpired { get; set; }
        private int TimeoutMilliseconds { get; set; }

        public static Timer StartNew(int milliseconds)
        {
            var timer = new Timer
            {
                StopWatch = Stopwatch.StartNew(),
                TimeoutMilliseconds = milliseconds
            };
            return timer;
        }

        public bool HasExpired
        {
            get
            {
                if (_hasExpired)
                    return true;

                if (StopWatch.ElapsedMilliseconds <= TimeoutMilliseconds)
                    return false;

                StopWatch.Stop();
                _hasExpired = true;
                return _hasExpired;
            }
            set
            {
                if (!value)
                {
                    StopWatch.Restart();
                    _hasExpired = false;
                }
                _hasExpired = true;
            }
        }
    }
}
