using EntityTableService;
using EntityTableService.Tests;
using EntityTableService.Tests.Models;
using Samples.Diagnostics;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Samples
{
    public static class EntityClientSample
    {
        private const int ENTITY_COUNT = 1;
        private const int ITERATION_COUNT = 1;
        private static string ConnectionString => Environment.GetEnvironmentVariable("ConnectionString") ?? "UseDevelopmentStorage=true";

        public static async Task Run()
        {
            var options = new EntityTableClientOptions(ConnectionString, $"{nameof(PersonEntity)}Table", maxConcurrentInsertionTasks: 10);
            var entityClient = new EntityTableClient<PersonEntity>(options, config =>
            {
                config
                .ComposePartitionKey(p => p.AccountId)
                .SetPrimaryKey(p => p.PersonId)
                .AddIndex(p => p.Created)
                .AddIndex(p => p.LastName)
                .AddIndex(p => p.Distance)
                .AddIndex(p => p.Enabled)
                .AddIndex(p => p.Latitude)
                .AddIndex(p => p.Longitude)
                .AddDynamicProp(nameof(PersonEntity.FirstName), p => p.FirstName.ToUpperInvariant())
                .AddDynamicProp("_IsInFrance", p => (p.Address.State == "France"))
                .AddDynamicProp("_MoreThanOneAddress", p => (p.OtherAddress.Count > 1))
                .AddDynamicProp("_CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6)))
                .AddDynamicProp("_FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3))

                .AddIndex("_FirstLastName3Chars");
            });

            var faker = Fakers.CreateFakedPerson();

            while (true)
            {
                Console.Write($"Generate faked {ENTITY_COUNT} entities...");
                var persons = faker.Generate(ENTITY_COUNT);
                Console.WriteLine("Ok");

                var counters = new PerfCounters(nameof(TableEntityBinderTests));
                Console.Write($"Insert {ENTITY_COUNT} entities...");
                using (var mesure = counters.Mesure($"{ENTITY_COUNT} insertions"))
                {
                    await entityClient.InsertMany(persons);
                }
                Console.WriteLine($"in {counters.Get()[$"{ENTITY_COUNT} insertions"].TotalDuration.TotalSeconds} seconds");
                counters.Clear();
                Console.WriteLine($"Querying entities {ITERATION_COUNT} times...");
                foreach (var person in persons.Take(ITERATION_COUNT))
                {
                    using (var mesure = counters.Mesure("1. Get By Id"))
                    {
                        _ = await entityClient.GetByIdAsync(person.AccountId, person.PersonId);
                    }

                    using (var mesure = counters.Mesure("2. Get By LastName"))
                    {
                        _ = await entityClient.GetAsync(
                                person.AccountId,
                                w => w.Where(p => p.LastName).Equal(person.LastName));
                    }

                    using (var mesure = counters.Mesure("3. Get By LastName (indexed)"))
                    {
                        _ = await entityClient.GetByAsync(
                                person.AccountId,
                                p => p.LastName,
                                person.LastName);
                    }

                    using (var mesure = counters.Mesure("4. Get LastName start with 'arm'"))
                    {
                        _ = await entityClient.GetAsync(
                                person.AccountId,
                                w => w.Where("_FirstLastName3Chars").Equal("arm"));
                    }

                    using (var mesure = counters.Mesure("5. Get by LastName start with 'arm' (indexed)"))
                    {
                        _ = await entityClient.GetByAsync(
                              person.AccountId,
                             "_FirstLastName3Chars", "arm");
                    }
                }

                Console.WriteLine("====================================");
                foreach (var counter in counters.Get().OrderBy(c => c.Key))
                {
                    WriteLineDuration($"{counter.Key} ", counter.Value);
                }
                Console.WriteLine("====================================");
            }
        }

        private static void WriteLineDuration(string text, IPerfCounter counter)
        {
            Console.Write(text);

            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = (counter.AverageDuration.TotalSeconds < 1) ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"{Math.Round(counter.AverageDuration.TotalSeconds, 3)} seconds");

            Console.ForegroundColor = prevColor;
        }
    }
}