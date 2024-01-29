using HA.Observable;
using System;
using System.Timers;

namespace HA.EhZ.Tests
{
    public class ObservableTest<T> : ObservableBase<T>
    {
        private readonly Timer _timer;

        public T? Value { get; set; }

        public ObservableTest()
        {
            _timer = new Timer { Interval = 5000, Enabled = false };
            _timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Value != null)
                ExecuteOnNext(Value);
            else
                ExecuteOnError(new ArgumentNullException("Value is null"));
        }

        public void Start()
        {
            _timer.Enabled= true;
        }

        public void Stop() 
        {
            _timer.Enabled= false;
        }
    }
}
