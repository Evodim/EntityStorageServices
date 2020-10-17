using System;
using System.Threading.Tasks;

namespace Samples
{
    public class Program
    {
        private static async Task Main()
        {
            await EntityClientSample.Run();
            Console.ReadLine();
        }
    }
}