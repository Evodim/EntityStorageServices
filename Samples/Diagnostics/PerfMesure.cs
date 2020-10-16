namespace Samples.Diagnostics
{
    using System;

    public class PerfMesure : IDisposable
    {
        private PerfCounters _counters;
        private string _blockname;
        private readonly object _syncLock = new object();
        private volatile bool _disposing;

        public PerfMesure(PerfCounters counters, string blockname)
        {
            _counters = counters;
            _blockname = blockname;
            _counters.In(_blockname);
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                if (_disposing)
                {
                    return;
                }

                _disposing = true;
            }

            _counters.Out(_blockname);
        }
    }
}