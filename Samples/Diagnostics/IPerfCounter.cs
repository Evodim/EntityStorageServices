namespace Samples.Diagnostics
{
    using System;

    public interface IPerfCounter
    {
        string Context { get; }
        string Name { get; }
        long MaxCount { get; }
        long MinCount { get; }
        long InCount { get; }
        long OutCount { get; }

        TimeSpan Duration();

        long In();

        long Out();
    }
}