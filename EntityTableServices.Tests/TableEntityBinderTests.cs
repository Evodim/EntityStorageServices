﻿using EntityTableService.AzureClient;
using EntityTableService.Tests.Helpers;
using EntityTableService.Tests.Models;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.ComponentModel;
using System.Threading.Tasks;


[assembly: Description("EntityTableService.Tests")]
namespace EntityTableService.Tests
{
    public class TableEntityBinderTests
    {
        private readonly CloudStorageAccount account;

        private readonly CloudTable cloudTable;

        private readonly CloudTableClient cloudTableClient;

        public TableEntityBinderTests()
        {
            account = CloudStorageAccount.Parse(ConnectionString);
            cloudTableClient = account.CreateCloudTableClient();
            cloudTable = cloudTableClient.GetTableReference(nameof(TableEntityBinderTests));
            var tbReq = new TableRequestOptions()
            {
                RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(1000), 3)
            };
            var tbContext = new OperationContext();
            cloudTable.CreateIfNotExistsAsync(tbReq, tbContext).GetAwaiter().GetResult();
        }

        private static string ConnectionString => Environment.GetEnvironmentVariable("ConnectionString") ?? "UseDevelopmentStorage=true";

        [PrettyFact]
        public async Task Should_Handle_Extented_Values_Wit_hBindable_Entity()
        {
            var partitionName = Guid.NewGuid().ToString();
            var person = Fakers.CreateFakedPerson().Generate();
            //decimal
            person.Altitude = 1.6666666666666666666666666667M;
            //float
            person.BankAmount = 2.00000024F;
            //enum
            person.Situation = Situation.Divorced;

            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());
            var entityResult = await UpSertAndRetrieve(tableEntity);

            entityResult.Entity.Altitude.Should().Be(person.Altitude);
            entityResult.Entity.BankAmount.Should().Be(person.BankAmount);
            entityResult.Entity.Situation.Should().Be(person.Situation);
        }

        [PrettyFact]
        public async Task Should_InsertOrMerge_Bindable_Entity()
        {
            var partitionName = Guid.NewGuid().ToString();

            var person = Fakers.CreateFakedPerson().Generate();
            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());

            _ = await UpSertAndRetrieve(tableEntity);

            tableEntity = new TableEntityBinder<PersonEntity>(new PersonEntity()
            {
                PersonId = person.PersonId,
                FirstName = "John Do",
                BirthDate = DateTime.UtcNow.AddYears(-25)

            }, partitionName, person.PersonId.ToString()); 

            var entityResult = await MergeAndRetrieve(tableEntity);

            //Only Nullable value and reference types are preserved in merge operation
            entityResult.Entity.LastName.Should().Be(person.LastName);
            entityResult.Entity.Latitude.Should().Be(default);
            entityResult.Entity.Longitude.Should().Be(default);
            entityResult.Entity.Altitude.Should().Be(person.Altitude);
            entityResult.Entity.BankAmount.Should().Be(person.BankAmount);
            entityResult.Entity.PersonId.Should().Be(person.PersonId.ToString());
            entityResult.Entity.FirstName.Should().Be("John Do");
        }

        [PrettyFact]
        public async Task Should_InsertOrReplace_Bindable_Entity()
        {
            var partitionName = Guid.NewGuid().ToString();

            var person = Fakers.CreateFakedPerson().Generate();

            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());

            var entityResult = await UpSertAndRetrieve(tableEntity);

            entityResult.RowKey.Should().Be(person.PersonId.ToString());

            entityResult.Entity.Should().BeEquivalentTo(person);
        }

        [PrettyFact]
        public async Task Should_InsertOrReplace_Metadatas_With_Bindable_Entity()
        {
            var partitionName = Guid.NewGuid().ToString();

            var person = Fakers.CreateFakedPerson().Generate();

            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());
            tableEntity.Metadatas.Add("_HasChildren", true);
            tableEntity.Metadatas.Add("_Deleted", false);

            _ = await UpSertAndRetrieve(tableEntity);

            tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());
            tableEntity.Metadatas.Add("_HasChildren", false);

            var entityResult = await UpSertAndRetrieve(tableEntity);
            entityResult.Entity.Should().BeEquivalentTo(person);

            entityResult.Metadatas.Should().Contain("_HasChildren", false);
            entityResult.Metadatas.Should().NotContainKey("_Deleted", because: "InsertOrReplace replace all entity props and it's metadatas");
        }

        [PrettyFact]
        public async Task Should_Merge_Metadatas_With_Bindable_Entity()
        {
            var partitionName = Guid.NewGuid().ToString();

            var person = Fakers.CreateFakedPerson().Generate();

            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());
            tableEntity.Metadatas.Add("_HasChildren", true);
            tableEntity.Metadatas.Add("_Deleted", true);

            _ = await UpSertAndRetrieve(tableEntity);

            tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());
            tableEntity.Metadatas.Add("_HasChildren", false);

            var entityResult = await MergeAndRetrieve(tableEntity);
            entityResult.Entity.Should().BeEquivalentTo(person);

            entityResult.Metadatas.Should().Contain("_HasChildren", false);
            entityResult.Metadatas.Should().ContainKey("_Deleted", because: "InserOrMerge preserve non updated prop and metadatas");
            entityResult.Metadatas.Should().Contain("_Deleted", true);
        }

        [PrettyFact]
        public async Task Should_Store_Nullable_Types_In_Bindable_Entity()
        {
            var partitionName = Guid.NewGuid().ToString();

            var person = Fakers.CreateFakedPerson().Generate();

            person.Altitude = null;
            person.Distance = default;
            person.Created = null;
            person.Situation = null;

            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());

            var opw = TableOperation.InsertOrReplace(tableEntity);
            var opr = TableOperation.Retrieve<TableEntityBinder<PersonEntity>>(partitionName, person.PersonId.ToString());
            await cloudTable.ExecuteAsync(opw);
            var result = await cloudTable.ExecuteAsync(opr);
            var entityResult = (result.Result as TableEntityBinder<PersonEntity>).Entity;

            entityResult.Altitude.Should().Be(person.Altitude);
            entityResult.Distance.Should().Be(person.Distance);
        }

        private async Task<TableEntityBinder<T>> MergeAndRetrieve<T>(TableEntityBinder<T> tableEntity)
         where T : class, new()
        {
            try
            {
                var opw = TableOperation.InsertOrMerge(tableEntity);
                var opr = TableOperation.Retrieve<TableEntityBinder<T>>(tableEntity.PartitionKey, tableEntity.RowKey);
                await cloudTable.ExecuteAsync(opw);
                var result = await cloudTable.ExecuteAsync(opr);
                return result.Result as TableEntityBinder<T>;
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"{nameof(TableEntityBinderTests)} exception, unable to arrange data", ex);
            }
        }

        private async Task<TableEntityBinder<T>> UpSertAndRetrieve<T>(TableEntityBinder<T> tableEntity)
         where T : class, new()
        {
            try
            {
                var opw = TableOperation.InsertOrReplace(tableEntity);
                var opr = TableOperation.Retrieve<TableEntityBinder<T>>(tableEntity.PartitionKey, tableEntity.RowKey);
                await cloudTable.ExecuteAsync(opw);
                var result = await cloudTable.ExecuteAsync(opr);
                return result.Result as TableEntityBinder<T>;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{nameof(TableEntityBinderTests)} exception, unable to arrange data", ex);
            }
        }
    }
}