﻿using EntityTableService;
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
        private const int ENTITY_COUNT = 100;

        private static string ConnectionString => Environment.GetEnvironmentVariable("ConnectionString") ?? "UseDevelopmentStorage=true";

        public static async Task Run()
        {
            var entityClient = EntityTableClient.Create<PersonEntity>(
            options =>
            {
                options
                .SetConnectionString(ConnectionString)
                .SetTableName($"{nameof(PersonEntity)}Table")
                .SetAutoCreateTable(true)
                .SetMaxItemsPerInsertion(1000)
                .SetMaxBatchedInsertionTasks(10);
            }

            , config =>
            {
                config

                //Partition key could be composed with any string based values
                .SetPartitionKey(p => p.AccountId)

                //Define an entity prop as primary key
                .SetPrimaryKey(p => p.PersonId)

                //Add additionnal indexes
                .AddIndex(p => p.LastName)
                .AddIndex(p => p.Distance)
                .AddIndex(p => p.Enabled)

                //props couldbe ignored (for both read and write operations)
                .AddIgnoredProp(p => p.Created)

                //Add computed props, computed on each updates.
                .AddComputedProp("_IsInFrance", p => (p.Address.State == "France"))
                .AddComputedProp("_MoreThanOneAddress", p => (p.OtherAddress.Count > 1))
                .AddComputedProp("_CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6)))
                .AddComputedProp("_FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3))

                //Native props values could be overrided by computed props
                .AddComputedProp(nameof(PersonEntity.FirstName), p => p.FirstName.ToUpperInvariant())

                //Add index for any entity or computed props
                .AddIndex("_FirstLastName3Chars");
            });

            var faker = Fakers.CreateFakedPerson();
            Console.Write($"Generate faked {ENTITY_COUNT} entities...");
            Console.WriteLine("Ok");
            var persons = faker.Generate(ENTITY_COUNT);

            var counters = new PerfCounters(nameof(TableEntityBinderTests));

            using (var mesure = counters.Mesure($"{ENTITY_COUNT} insertions"))
            {
                await entityClient.InsertOrReplaceAsync(persons);
            }

            using (var mesure = counters.Mesure($"{ENTITY_COUNT} merged"))
            {
                await entityClient.InsertOrMergeAsync(persons);
            }

            counters.Clear();

            var person = persons.FirstOrDefault();

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

            Console.WriteLine("====================================");
            foreach (var counter in counters.Get().OrderBy(c => c.Key))
            {
                WriteLineDuration($"{counter.Key} ", counter.Value);
            }
            Console.WriteLine("====================================");

            //Iterate all entities from the table without memory pressure

            //long count = 0;
            //Console.WriteLine("Get all entities");
            //await foreach (var page in entityClient.GetAllAsync())
            //{
            //    count += page.Count();
            //    Console.WriteLine($"{count} entities readed");
            //}
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