using EntityTableService.AzureClient;
using EntityTableService.ExpressionHelpers;
using EntityTableService.Tests;
using EntityTableService.Tests.Models;
using Samples.Diagnostics;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Samples
{
    public class EntityClientSample
    {
        private const int ENTITY_COUNT = 100;
        private const int OPERATION_COUNT = 10;
        private const string connectionString = "UseDevelopmentStorage=true";

        public static async Task Run()
        {
            static string partitionKey(string accountId) => $"Account-{accountId}";

            IEntityTableClient<PersonEntity> entityClient = new EntityTableClient<PersonEntity>(
            new EntityTableClientOptions(connectionString, $"{nameof(PersonEntity)}Table", maxConcurrentInsertionTasks: 10),
            c =>
            {
                c.SetPartitionResolver(e => partitionKey(e.AccountId));
                c.SetPrimaryKey(p => p.PersonId);
                c.AddIndex(p => p.Created);
                c.AddIndex(p => p.LastName);
                c.AddIndex(p => p.Enabled);
                c.AddIndex(p => p.Distance);
                c.AddIndex("_FirstLastName3Chars");
                c.AddDynamicProp("_IsInFrance", p => (p.Address.State == "France"));
                c.AddDynamicProp("_MoreThanOneAddress", p => (p.OtherAddress.Count > 1));
                c.AddDynamicProp("_CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6)));
                c.AddDynamicProp("_FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3));
            }
         );

            var faker = Fakers.CreateFakedPerson();
            while (true)
            {
                var persons = faker.Generate(ENTITY_COUNT);

                Console.Write($"Generate faked {ENTITY_COUNT} entities...");
                Console.WriteLine("Ok");

                var counters = new PerfCounters(nameof(TableEntityBinderTests));
                Console.Write($"Insert {ENTITY_COUNT} entities...");
                using (var mesure = counters.Mesure($"{ENTITY_COUNT} insertions"))
                {
                    await entityClient.InsertOrReplaceAsync(persons);
                }
                Console.WriteLine($"in {counters.Get()[$"{ENTITY_COUNT} insertions"].Duration().TotalSeconds} seconds");
                counters.Clear();

                //Get entities with primary prop
                using (var mesure = counters.Mesure("Get By Id"))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                        var result = await entityClient.GetByIdAsync(partitionKey(person.AccountId), person.PersonId);
                    }
                }
                //Get entities with indexed prop
                using (var mesure = counters.Mesure("Get By LastName (indexed)"))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                        var result = await entityClient.GetByAsync(
                            partitionKey(person.AccountId),
                            p => p.LastName,
                            person.LastName,
                            w => w.Where(p => p.LastName).Equal(person.LastName));
                    }
                }
                //Get entities with not indexed prop
                using (var mesure = counters.Mesure("Get By LastName (not indexed)"))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                        var result = await entityClient.GetAsync(
                            partitionKey(person.AccountId),
                            w => w.Where(p => p.LastName).Equal(person.LastName));
                    }
                }

                //Get entities with not indexed dynamic prop
                using (var mesure = counters.Mesure("Get LastName start with 'arm' (not indexed) "))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                        var result = await entityClient.GetAsync(partitionKey(person.AccountId),
                             w => w.Where("_FirstLastName3Chars").Equal("arm"));
                    }
                }
                //Get entities with indexed dynamic prop
                using (var mesure = counters.Mesure("Get LastName start with 'arm' "))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                        var result = await entityClient.GetByAsync(partitionKey(person.AccountId),
                            "_FirstLastName3Chars", "arm");
                    }
                }
                Console.WriteLine("====================================");
                foreach (var counter in counters.Get())
                {
                    WriteLineDuration($"{counter.Key} :", counter.Value.Duration());
                }
                Console.WriteLine("====================================");
            }
        }

        private static void WriteLineDuration(string text, TimeSpan duration)
        {
            Console.Write(text);

            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = (duration.TotalSeconds < 1) ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"{Math.Round(duration.TotalSeconds, 3)} seconds");

            Console.ForegroundColor = prevColor;
        }
    }
}