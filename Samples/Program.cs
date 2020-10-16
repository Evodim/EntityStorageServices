using System;
using System.Threading.Tasks;

namespace Samples
{
    public class Program
    {
        public const string connectionString = "UseDevelopmentStorage=true";

        private static async Task Main(string[] args)
        {
            await EntityClientSample.Run();
            Console.ReadLine();
        }
    }
}