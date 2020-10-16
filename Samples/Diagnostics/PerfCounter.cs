namespace Samples.Diagnostics
{
    using System;
    using System.Threading;

    /// <example>
    /// //Basic example
    /// var counters = new MetricCounters("TestLive");
    /// using(var mesure = counters.Mesure("readByPartyId_Keyed"))
    ///	{
    ///	}
    /// foreach(var counter in counters.Get())
    /// {
    ///	Console.WriteLine($"{counter.Key} {counter.Value.Duration().TotalSeconds} seconds");
    /// }
    /// </example>
    public class PerfCounter : IPerfCounter
    {
        private long _inCount = 0;
        private long _outCount = 0;
        private long _maxCount = 0;
        private long _minCount = 0;
        private long _inTicks = 0;
        private long _outTicks = 0;

        private readonly string _context = "";
        private readonly string _name = "";

        string IPerfCounter.Name => _name;
        public long MaxCount => _maxCount;
        public long MinCount => _minCount;
        public long InCount => _inCount;
        public long OutCount => _outCount;

        public PerfCounter(string topic, string name)
        {
            _context = topic;
            _name = name;
        }

        public long In()
        {
            Interlocked.Exchange(ref _inTicks, DateTimeOffset.UtcNow.Ticks);
            Interlocked.Exchange(ref _outTicks, DateTimeOffset.UtcNow.Ticks);
            return Interlocked.Increment(ref _inCount);
        }

        public long Out()
        {
            Interlocked.Exchange(ref _outTicks, DateTimeOffset.UtcNow.Ticks);

            return Interlocked.Increment(ref _outCount);
        }

        public TimeSpan Duration() => new TimeSpan(_outTicks - _inTicks);

        public string Context => _context;

        protected void UpdateState()
        {
            if (_inCount > _maxCount) Interlocked.Exchange(ref _maxCount, _inCount);
            if (_inCount < _minCount) Interlocked.Exchange(ref _minCount, _inCount);
        }
    }
}