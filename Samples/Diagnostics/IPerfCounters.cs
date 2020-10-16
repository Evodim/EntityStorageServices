namespace Samples.Diagnostics
{
    using System;
    using System.Collections.Concurrent;

    public interface IPerfCounters
    {
        ConcurrentDictionary<string, IPerfCounter> Get();

        long In(string name);

        IDisposable Mesure(string name);

        long Out(string name);
    }
}