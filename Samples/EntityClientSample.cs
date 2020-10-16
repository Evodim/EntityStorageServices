using EntityTableService.Tests;
using EntityTableService.Tests.Models;
using Evod.Toolkit.Azure.Storage;
using Samples.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples
{
    public class EntityClientSample
    {
        private const int ENTITY_COUNT = 1;
        private const string connectionString = "UseDevelopmentStorage=true";

        public static async Task Run()
        {
            Func<string, string> partitionKey = accountId => $"Account-{accountId}";
            var entityClient = new EntityTableClient<PersonEntity>(
               new EntityTableClientOptions(connectionString, "TestTable", MaxConcurrentIntertionTasks: 10),
               c =>
               {
                   c.SetPartitionResolver(e => partitionKey(e.AccountId));
                   c.SetPrimaryKey(p => p.PersonId);
                   c.AddIndex(p => p.Created);
                   c.AddIndex(p => p.LastName);
                   c.AddIndex(p => p.Enabled);
                   c.AddIndex(p => p.Distance);
                   c.AddDynamicProp("IsInFrance", p => (p.Address.State == "France") ? true : false);
                   c.AddDynamicProp("MoreThanOneAddress", p => (p.OtherAddress.Count > 1) ? true : false);
                   c.AddDynamicProp("CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6) ? true : false));
                   c.AddDynamicProp("FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3));
               }
            );
            var faker = Fakers.CreateFakedPerson();
            var persons = faker.Generate(ENTITY_COUNT);

            while (true)
            {
                persons.ForEach(p =>
                {
                    p.AccountId = "" + new Random().Next(1, 9);
                });
                IEnumerable<PersonEntity> result;
                var counters = new PerfCounters(nameof(TableEntityBinderTests));

                using (var mesure = counters.Mesure($"{ENTITY_COUNT} insertions"))
                {
                    await entityClient.InsertOrReplace(persons);
                }
                using (var mesure = counters.Mesure("Get By Id"))
                {
                    foreach (var person in persons.Take(ENTITY_COUNT))
                    {
                        var current = await entityClient.GetByIdAsync(partitionKey(person.AccountId), person.PersonId);
                        // current.CreationDate = default;
                        await entityClient.InsertOrMerge(current);
                        var updated = entityClient.GetByIdAsync(partitionKey(person.AccountId), person.PersonId);
                    }
                }

                using (var mesure = counters.Mesure("Get By LastName (indexed)"))
                {
                    foreach (var person in persons.Take(ENTITY_COUNT))
                    {
                        var b = await entityClient.GetByIndexAsync(
                            partitionKey(person.AccountId),
                            p => p.LastName,
                            person.LastName,
                            w => w.Where(p => p.LastName).Equal(person.LastName));
                    }
                }
                using (var mesure = counters.Mesure("Get By Enabled (indexed)"))
                {
                    foreach (var person in persons.Take(ENTITY_COUNT))
                    {
                        var b = await entityClient.GetByIndexAsync(partitionKey(person.AccountId), p => p.Enabled, person.Enabled);
                    }
                }
                using (var mesure = counters.Mesure("Get props"))
                {
                    foreach (var person in persons.Take(ENTITY_COUNT))
                    {
                        var props = await entityClient.GetPropsAsync(partitionKey(person.AccountId), new string[] { "Enabled" },
                            w => w.Where(p => p.Enabled).Equal(person.Enabled));
                    }
                }
                var firstPerson = persons.First();

                using (var mesure = counters.Mesure("Get big list"))
                {
                    var list = await entityClient.GetAsync(partitionKey(firstPerson.AccountId), w => w.Where(p => p.Enabled).Equal(false));
                    Console.WriteLine($"{list.Count()} fetched");
                }
                foreach (var counter in counters.Get())
                {
                    Console.WriteLine("====================================");
                    Console.WriteLine($"{counter.Key} | {counter.Value.Duration().TotalSeconds} seconds");
                }
            }
        }
    }
}