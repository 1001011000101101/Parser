using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Timers;

namespace Parser.Shared.Models
{
    public class BlazorTimer
    {
        private Timer _timer;
        private bool renewable;

        public void SetTimer(double interval, bool renewable = false)
        {
            _timer = new Timer(interval);
            _timer.Elapsed += NotifyTimerElapsed;
            _timer.Enabled = true;
            this.renewable = renewable;
        }

        public void StopTimer()
        {
            if (_timer == null) return;

            _timer.Enabled = false;
        }

        public event Action OnElapsed;

        private void NotifyTimerElapsed(Object source, ElapsedEventArgs e)
        {
            OnElapsed?.Invoke();

            if (!renewable)
            {
                _timer.Dispose();
            }
            
        }
    }
}
