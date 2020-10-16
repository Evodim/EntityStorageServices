﻿namespace Samples.Diagnostics
{
    using System;
    using System.Collections.Concurrent;

    public class PerfCounters : IPerfCounters
    {
        private readonly string _context = "";
        private readonly ConcurrentDictionary<string, IPerfCounter> _blockCounters = new ConcurrentDictionary<string, IPerfCounter>();

        public PerfCounters(string context)
        {
            _context = context;
        }

        public long In(string name)
        {
            if (!_blockCounters.ContainsKey(name)) _blockCounters.TryAdd(name, new PerfCounter(_context, name));
            return _blockCounters[name].In();
        }

        public long Out(string name)
        {
            if (!_blockCounters.ContainsKey(name)) _blockCounters.TryAdd(name, new PerfCounter(_context, name));
            return _blockCounters[name].Out();
        }

        public IDisposable Mesure(string purpose)
        {
            return new PerfMesure(this, purpose);
        }

        public ConcurrentDictionary<string, IPerfCounter> Get()
        {
            return _blockCounters;
        }
    }
}