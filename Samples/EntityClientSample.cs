using EntityTableService.AzureClient;
using EntityTableService.Tests;
using EntityTableService.Tests.Models;
using Samples.Diagnostics;
using System;
using System.Linq;
using System.Threading.Tasks;
using EntityTableService.ExpressionHelpers;
using System.Runtime.CompilerServices;

namespace Samples
{
    public class EntityClientSample
    {
        private const int ENTITY_COUNT = 1000;
        private const int OPERATION_COUNT = 10;
        private const string connectionString = "UseDevelopmentStorage=true";

        public static async Task Run()
        {
            static string partitionKey(string accountId) => $"Account-{accountId}";

            var entityClient = new EntityTableClient<PersonEntity>(
               new EntityTableClientOptions(connectionString, "TestTable", maxConcurrentInsertionTasks: 10),
               c =>
               {
                   c.SetPartitionResolver(e => partitionKey(e.AccountId));
                   c.SetPrimaryKey(p => p.PersonId);
                   c.AddIndex(p => p.Created);
                   c.AddIndex(p => p.LastName);
                   c.AddIndex(p => p.Enabled);
                   c.AddIndex(p => p.Distance);
                   c.AddDynamicProp("IsInFrance", p => (p.Address.State == "France"));
                   c.AddDynamicProp("MoreThanOneAddress", p => (p.OtherAddress.Count > 1));
                   c.AddDynamicProp("CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6)));
                   c.AddDynamicProp("FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3));
               }
            );
           
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
                    await entityClient.InsertOrReplace(persons);
                  
                }
                Console.WriteLine($"in {counters.Get()[$"{ENTITY_COUNT} insertions"].Duration().TotalSeconds} seconds");
                counters.Clear();

                //Get entity by id according to PrimaryKey entity configuration
                using (var mesure = counters.Mesure("Get By Id"))
                {
                    foreach (var person in persons.Take(10))
                    {
                        var result = await entityClient.GetByIdAsync(partitionKey(person.AccountId), person.PersonId);
                    }
                }
                //Get entities by using a prop filter (indexed)
                using (var mesure = counters.Mesure("Get By LastName (indexed)"))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                        var result = await entityClient.GetByIndexAsync(
                            partitionKey(person.AccountId),
                            p => p.LastName,
                            person.LastName,
                            w => w.Where(p => p.LastName).Equal(person.LastName));
                    }
                }
                //Get entities by using a prop filter (not indexed)
                using (var mesure = counters.Mesure("Get By LastName (not indexed)"))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                        var b = await entityClient.GetAsync(
                            partitionKey(person.AccountId),
                            w => w.Where(p => p.LastName).Equal(person.LastName));
                    }
                }

                //Get entities by using dynamic prop filter
                using (var mesure = counters.Mesure("Get LastName start with 'arm' (dynamic props)"))
                {
                    foreach (var person in persons.Take(OPERATION_COUNT))
                    {
                       var result = await entityClient.GetAsync(partitionKey(person.AccountId),
                            w => w.Where("_FirstLastName3Chars").Equal("arm"));
                    }
                }
                Console.WriteLine("====================================");
                foreach (var counter in counters.Get())
                {
                    WriteLineDuration($"{counter.Key} :",counter.Value.Duration());
                }
                Console.WriteLine("====================================");
            }
        }
        private static void WriteLineDuration(string text, TimeSpan duration) {
            Console.Write(text);

            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor =(duration.TotalSeconds<1)? ConsoleColor.Green:ConsoleColor.Yellow;
            Console.WriteLine($"{Math.Round(duration.TotalSeconds,3)} seconds");

            Console.ForegroundColor = prevColor;

        }
    }
}