using EntityTableService.AzureClient;
using EntityTableService.Tests.Helpers;
using EntityTableService.Tests.Models;
using FluentAssertions;
using FluentAssertions.Common;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EntityTableService.Tests
{
    public class EntityTableClientTests
    {
    
        private readonly EntityTableClientOptions _commonOptions;

        private string ConnectionString => "UseDevelopmentStorage=true";

        public EntityTableClientTests()
        {
            _commonOptions = new EntityTableClientOptions()
            {
                ConnectionString = ConnectionString,
                MaxBatchedInsertionTasks = 1,
                TableName = nameof(EntityTableClientTests)
            };

        }
       
        [PrettyFact(DisplayName = nameof(ShouldSetPrimaryKeyOnInsertOrUpdate))]
        public async Task ShouldSetPrimaryKeyOnInsertOrUpdate()
        {
            var partitionName = Guid.NewGuid().ToString();
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = new EntityTableClient<PersonEntity>(_commonOptions, c => {
                c.SetPartitionResolver(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByIdAsync(person.AccountId, person.PersonId);
            created.Should().BeEquivalentTo(person);
        }

        [PrettyFact(DisplayName = nameof(ShouldSetPropIndexOnInsertOrUpdate))]
        public async Task ShouldSetPropIndexOnInsertOrUpdate()
        {
            var partitionName = Guid.NewGuid().ToString();
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = new EntityTableClient<PersonEntity>(_commonOptions, c => {
                c.SetPartitionResolver(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
                c.AddIndex(p => p.LastName);

            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByAsync(person.AccountId, p=> p.LastName,person.LastName);
            created.FirstOrDefault()?.Should().BeEquivalentTo(person);

      }

        [PrettyFact(DisplayName = nameof(ShouldSetDynamicPropOnInsertOrUpdate))]
        public async Task ShouldSetDynamicPropOnInsertOrUpdate()
        {
            Func<string,string> First3Char = s => s.ToLower().Substring(0, 3);

            var partitionName = Guid.NewGuid().ToString();
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = new EntityTableClient<PersonEntity>(_commonOptions, c => {
                c.SetPartitionResolver(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
                c.AddDynamicProp("_FirstLastName3Chars",p=> First3Char(p.LastName));

            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByIdAsync(person.AccountId, person.PersonId);
            First3Char(created.LastName).Should().Be(First3Char(person.LastName));
        }
        [PrettyFact(DisplayName = nameof(ShouldSetComputedIndexOnInsertOrUpdate))]
        public async Task ShouldSetComputedIndexOnInsertOrUpdate()
        {
            Func<string, string> First3Char = s => s.ToLower().Substring(0, 3);

            var partitionName = Guid.NewGuid().ToString();
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = new EntityTableClient<PersonEntity>(_commonOptions, c => {
                c.SetPartitionResolver(p => p.AccountId);
                c.SetPrimaryKey(p => p.PersonId);
                c.AddDynamicProp("_FirstLastName3Chars", p => First3Char(p.LastName));
                c.AddIndex("_FirstLastName3Chars");

            });

            await tableEntity.InsertOrReplaceAsync(person);
            var created = await tableEntity.GetByAsync(person.AccountId, "_FirstLastName3Chars", First3Char(person.LastName));
            First3Char(created.FirstOrDefault().LastName).Should().Be(First3Char(person.LastName));
        }

        [PrettyFact(DisplayName = nameof(ShouldRemoveIndexesOnDelete))]
        public async Task ShouldRemoveIndexesOnDelete()
        {
            Func<string, string> First3Char = s => s.ToLower().Substring(0, 3);

            var partitionName = Guid.NewGuid().ToString();
            var person = Fakers.CreateFakedPerson().Generate();
            person.AccountId = Guid.NewGuid().ToString();
            var tableEntity = new EntityTableClient<PersonEntity>(_commonOptions, c => {
                c.SetPartitionResolver(p => p.AccountId);
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
            (await tableEntity.GetByAsync(person.AccountId, p=>p.LastName,person.LastName)).Should().BeEmpty();




        }
         
    }
}