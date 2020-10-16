using EntityTableService.Tests.Helpers;
using EntityTableService.Tests.Models;
using Evod.Toolkit.Azure.Storage;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Threading.Tasks;

namespace EntityTableService.Tests
{
    public class TableEntityBinderTests
    {
        private readonly CloudStorageAccount account;

        private readonly CloudTable cloudTable;

        private readonly CloudTableClient cloudTableClient;

        private readonly EntityTableClient<PersonEntity> entityClient;

        private string connectionString => "UseDevelopmentStorage=true";

        public TableEntityBinderTests()
        {
            account = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = account.CreateCloudTableClient();
            cloudTable = cloudTableClient.GetTableReference("TestTable");
            var tbReq = new TableRequestOptions()
            {
                RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(1000), 3)
            };
            var tbContext = new OperationContext();
            cloudTable.CreateIfNotExistsAsync(tbReq, tbContext).GetAwaiter().GetResult();
        }

        [PrettyFact(DisplayName = nameof(ShouldInsertOrMergeBindableEntity))]
        public async Task ShouldInsertOrMergeBindableEntity()
        {
            var partitionName = "partitionKey1";

            var person = Fakers.CreateFakedPerson().Generate();
            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());

            var opw = TableOperation.InsertOrReplace(tableEntity);
            await cloudTable.ExecuteAsync(opw);

            tableEntity = new TableEntityBinder<PersonEntity>(new PersonEntity() { FirstName = "John Do" }, partitionName, person.PersonId.ToString());
            tableEntity.Timestamp = DateTimeOffset.UtcNow;
            opw = TableOperation.InsertOrMerge(tableEntity);
            await cloudTable.ExecuteAsync(opw);

            var opr = TableOperation.Retrieve<TableEntityBinder<PersonEntity>>(partitionName, person.PersonId.ToString());
            var result = await cloudTable.ExecuteAsync(opr);

            (result.Result as TableEntityBinder<PersonEntity>).RowKey.Should().Be(person.PersonId.ToString());
            result.Result?.Should().BeOfType<TableEntityBinder<PersonEntity>>();
            var entityResult = (result.Result as TableEntityBinder<PersonEntity>).OriginalEntity;
            entityResult.FirstName.Should().Be("John Do");
        }

        [PrettyFact(DisplayName = nameof(ShouldInsertOrReplaceBindableEntity))]
        public async Task ShouldInsertOrReplaceBindableEntity()
        {
            var partitionName = "partitionKey1";

            var person = Fakers.CreateFakedPerson().Generate();

            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());

            var opw = TableOperation.InsertOrReplace(tableEntity);
            var opr = TableOperation.Retrieve<TableEntityBinder<PersonEntity>>(partitionName, person.PersonId.ToString());
            await cloudTable.ExecuteAsync(opw);
            var result = await cloudTable.ExecuteAsync(opr);

            (result.Result as TableEntityBinder<PersonEntity>).RowKey.Should().Be(person.PersonId.ToString());
            result.Result?.Should().BeOfType<TableEntityBinder<PersonEntity>>();
            var entityResult = (result.Result as TableEntityBinder<PersonEntity>).OriginalEntity;
            entityResult.Should().BeEquivalentTo(person);
        }

        [PrettyFact(DisplayName = nameof(ShouldInsertOrReplaceMetadatasWithBindableEntity))]
        public async Task ShouldInsertOrReplaceMetadatasWithBindableEntity()
        {
            var partitionName = "partitionKey1";

            var person = Fakers.CreateFakedPerson().Generate();

            var tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());
            tableEntity.Metadatas.Add("_HasChildren", true);
            tableEntity.Metadatas.Add("_Deleted", false);

            var opw = TableOperation.InsertOrReplace(tableEntity);
            var opr = TableOperation.Retrieve<TableEntityBinder<PersonEntity>>(partitionName, person.PersonId.ToString());
            await cloudTable.ExecuteAsync(opw);
            var result = await cloudTable.ExecuteAsync(opr);

            (result.Result as TableEntityBinder<PersonEntity>).RowKey.Should().Be(person.PersonId.ToString());
            result.Result?.Should().BeOfType<TableEntityBinder<PersonEntity>>();
            var entityResult = (result.Result as TableEntityBinder<PersonEntity>).OriginalEntity;
            entityResult.Should().BeEquivalentTo(person);

            (result.Result as TableEntityBinder<PersonEntity>).Metadatas.Should().Contain("_HasChildren", true);
            (result.Result as TableEntityBinder<PersonEntity>).Metadatas.Should().Contain("_Deleted", false);
        }

        [PrettyFact(DisplayName = nameof(ShouldMergeMetadatasWithBindableEntity))]
        public async Task ShouldMergeMetadatasWithBindableEntity()
        {
            var partitionName = "partitionKey1";

            var person = Fakers.CreateFakedPerson().Generate();

            var tableEntity = new TableEntityBinder<PersonEntity>(person);
            tableEntity.PartitionKey = partitionName;
            tableEntity.RowKey = person.PersonId.ToString();

            tableEntity.Metadatas.Add("_HasChildren", true);
            tableEntity.Metadatas.Add("_Deleted", false);

            var opw = TableOperation.InsertOrReplace(tableEntity);
            await cloudTable.ExecuteAsync(opw);

            tableEntity = new TableEntityBinder<PersonEntity>(person, partitionName, person.PersonId.ToString());
            tableEntity.Metadatas.Add("_HasChildren", false);

            opw = TableOperation.InsertOrMerge(tableEntity);
            var opr = TableOperation.Retrieve<TableEntityBinder<PersonEntity>>(partitionName, person.PersonId.ToString());
            await cloudTable.ExecuteAsync(opw);
            var result = await cloudTable.ExecuteAsync(opr);

            (result.Result as TableEntityBinder<PersonEntity>).RowKey.Should().Be(person.PersonId.ToString());
            result.Result?.Should().BeOfType<TableEntityBinder<PersonEntity>>();
            var entityResult = (result.Result as TableEntityBinder<PersonEntity>).OriginalEntity;
            entityResult.Should().BeEquivalentTo(person);

            (result.Result as TableEntityBinder<PersonEntity>).Metadatas.Should().Contain("_HasChildren", false);
            (result.Result as TableEntityBinder<PersonEntity>).Metadatas.Should().Contain("_Deleted", false);
        }
    }
}