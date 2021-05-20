using EntityTableService.Tests.Helpers;
using EntityTableService.Tests.Models;
using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EntityTableService.Tests
{
    public class EntityTableClientTests
    {
        private readonly EntityTableClientOptions _commonOptions;

        private static string ConnectionString => "UseDevelopmentStorage=true";

        public EntityTableClientTests()
        {
            _commonOptions = new EntityTableClientOptions()
            {
                ConnectionString = ConnectionString,
                MaxBatchedInsertionTasks = 1,
                MaxItemsPerInsertion = 1000,
                TableName = nameof(EntityTableClientTests)
            };
        }

        [PrettyFact]
        public async Task Should_Gey_By_Indexed_Prop_With_Filter()
        {
            var partitionName = Guid.NewGuid().ToString();
            var persons = Fakers.CreateFakedPerson().Generate(50);

            persons.ForEach(p => p.AccountId = partitionName);
            var customOptions = new EntityTableClientOptions()
            {
                MaxItemsPerInsertion = 1,
                MaxBatchedInsertionTasks = 1,
                ConnectionString = _commonOptions.ConnectionString,
                TableName = _commonOptions.TableName
            };

            IEntityTableClient<PersonEntity> tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(customOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId)
                .SetPrimaryKey(p => p.PersonId)
                .AddIndex(p => p.LastName)
                .AddIndex(p => p.Created);
            });
            await tableEntity.InsertMany(persons);

            var person = persons.First();
            //get all entities both primary and projected
            var result = await tableEntity.GetByAsync(person.AccountId, p => p.Created, person.Created, filter: p => p.Where(p => p.Rank).Equal(person.Rank));
            result.Should().BeEquivalentTo(person);
        }

        [PrettyFact]
        public async Task Should_Set_Primary_Key_On_InsertOrUpdate()
        {
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(_commonOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByIdAsync(person.AccountId, person.PersonId);
            created.Should().BeEquivalentTo(person);
        }

        [PrettyFact]
        public async Task Should_Set_Prop_Index_On_InsertOrUpdate()
        {
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(_commonOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
                c.AddIndex(p => p.LastName);
            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByAsync(person.AccountId, p => p.LastName, person.LastName);
            created.FirstOrDefault()?.Should().BeEquivalentTo(person);
        }

        [PrettyFact]
        public async Task Should_Set_Dynamic_Prop_On_InsertOrUpdate()
        {
            static string First3Char(string s) => s.ToLower().Substring(0, 3);

            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(_commonOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
                c.AddDynamicProp("_FirstLastName3Chars", p => First3Char(p.LastName));
            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByIdAsync(person.AccountId, person.PersonId);
            First3Char(created.LastName).Should().Be(First3Char(person.LastName));
        }

        [PrettyFact]
        public async Task Should_Set_Computed_Index_On_InsertOrUpdate()
        {
            static string First3Char(string s) => s.ToLower().Substring(0, 3);
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(_commonOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
                c.AddDynamicProp("_FirstLastName3Chars", p => First3Char(p.LastName));
                c.AddIndex("_FirstLastName3Chars");
            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByAsync(person.AccountId, "_FirstLastName3Chars", First3Char(person.LastName));
            First3Char(created.FirstOrDefault()?.LastName).Should().Be(First3Char(person.LastName));
        }

        [PrettyFact]
        public async Task Should_Remove_Indexes_OnDelete()
        {
            static string First3Char(string s) => s.ToLower().Substring(0, 3);

            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(_commonOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
                c.AddIndex("_FirstLastName3Chars");
                c.AddIndex(p => p.LastName);
                c.AddDynamicProp("_FirstLastName3Chars", p => First3Char(p.LastName));
            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByIdAsync(person.AccountId, person.PersonId);
            await tableEntity.DeleteAsync(created);

            (await tableEntity.GetByIdAsync(person.AccountId, person.PersonId)).Should().BeNull();
            (await tableEntity.GetByAsync(person.AccountId, "_FirstLastName3Chars", First3Char(person.LastName))).Should().BeEmpty();
            (await tableEntity.GetByAsync(person.AccountId, p => p.LastName, person.LastName)).Should().BeEmpty();
        }

        [PrettyFact]
        public async Task Should_Observe_Entity_Table_Updates()
        {
            var partitionName = Guid.NewGuid().ToString();
            var persons = Fakers.CreateFakedPerson().Generate(10);
            var observer = new DummyObserver();

            persons.ForEach(p => p.AccountId = partitionName);

            var tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(_commonOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId)
                .SetPrimaryKey(p => p.PersonId)
                .AddObserver(nameof(DummyObserver), observer);
            });

            await tableEntity.InsertMany(persons);

            await tableEntity.DeleteAsync(persons.Skip(1).First());

            observer.CreatedCount.Should().Be(10);
            observer.Persons.Should().HaveCount(9);
            observer.DeletedCount.Should().Be(1);
        }

        [PrettyFact]
        public async Task Should_Insert_Indexed_Range_Entities()
        {
            var partitionName = Guid.NewGuid().ToString();
            var persons = Fakers.CreateFakedPerson().Generate(13);

            persons.ForEach(p => p.AccountId = partitionName);
            var customOptions = new EntityTableClientOptions()
            {
                MaxItemsPerInsertion = 1,
                MaxBatchedInsertionTasks = 1,
                ConnectionString = _commonOptions.ConnectionString,
                TableName = _commonOptions.TableName
            };

            IEntityTableClient<PersonEntity> tableEntity = EntityTableClient.CreateEntityTableClient<PersonEntity>(customOptions, c =>
            {
                c.ComposePartitionKey(p => p.AccountId)
                .SetPrimaryKey(p => p.PersonId)
                .AddIndex(p => p.LastName)
                .AddIndex(p => p.Created);
            });
            await tableEntity.InsertMany(persons);
            //get all entities both primary and projected
            var result = await tableEntity.GetAsync(partitionName);
            result.Should().HaveCount(13 * (1 + 2), because: "Inserted entities should generate 2 additional items as index projection");
        }
    }
}