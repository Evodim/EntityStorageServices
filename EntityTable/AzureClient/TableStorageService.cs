using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;
using Microsoft.Azure.Cosmos.Table.Queryable;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    public abstract class TableStorageService<T>
        where T : ITableEntity, new()
    {
        private readonly int _maxAttempts;
        private readonly int _waitAndRetrySeconds;
        private readonly bool _autoCreateTable;
        private readonly AsyncRetryPolicy _retryPolicy;
        protected CloudStorageAccount StorageAccount;
        protected CloudTable Table;
        protected CloudTableClient TableClient;
        protected string TableName;
        protected TableRequestOptions TableRequestOptions;

        protected TableStorageService(
            string tableName,
            string storageConnectionString,
            int maxAttempts = 10,
            int waitAndRetrySeconds = 1,
            bool autoCreateTable=false,
            TableRequestOptions tableRequestOptions = default)
        {

            _maxAttempts = maxAttempts;
            _waitAndRetrySeconds = waitAndRetrySeconds;
            _autoCreateTable = autoCreateTable;
            _retryPolicy = (autoCreateTable) ?

               Policy.Handle<StorageException>(e => HandleStorageException(e))
               .WaitAndRetryAsync(maxAttempts,
               i => TimeSpan.FromSeconds(_waitAndRetrySeconds),
               async (a, t) => await CreateTableIfNotExistsAsync()) :

               Policy.Handle<StorageException>(e => HandleStorageException(e))
               .WaitAndRetryAsync(maxAttempts,
               i => TimeSpan.FromSeconds(_waitAndRetrySeconds));


            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);

            var tableServicePoint = ServicePointManager.FindServicePoint(StorageAccount.TableEndpoint);
            //few optimizations specifics for storage requests
            tableServicePoint.UseNagleAlgorithm = false;
            tableServicePoint.Expect100Continue = false;
            tableServicePoint.ConnectionLimit = 100;

            // Create the table client.
            TableClient = StorageAccount.CreateCloudTableClient();
            Table = TableClient.GetTableReference(tableName);

            TableName = tableName;

            //set default options if not provided
            TableRequestOptions = tableRequestOptions ?? new TableRequestOptions()
            {
                RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(_waitAndRetrySeconds), _maxAttempts),
                // For Read-access geo-redundant storage, use PrimaryThenSecondary.
                // Otherwise set this to PrimaryOnly.
                LocationMode = LocationMode.PrimaryOnly

                // Maximum execution time based on the business use case.
                //MaximumExecutionTime = TimeSpan.FromSeconds(10) //not user yet ,if used , may raise timeout exceptions for huge requests
            };
            TableClient.DefaultRequestOptions = TableRequestOptions;
        }

        protected BatchedTableClient CreateBatchedClient(int batchedTasks) => new BatchedTableClient(TableName, StorageAccount, batchedTasks, autoCreateTable: _autoCreateTable);

        protected T CreateEntity(string partitionKey, string rowKey)
        {
            var newEntity = new T
            {
                PartitionKey = partitionKey,
                RowKey = rowKey
            };

            return newEntity;
        }

      
        protected async Task<bool> CreateTableIfNotExistsAsync()
        {
            var created = await Table.CreateIfNotExistsAsync();

            //Prevent Table operation delai
            if (created)
            {
                var nbretry = _maxAttempts;
                while (!await Table.ExistsAsync() && nbretry-- > 0) await Task.Delay(_waitAndRetrySeconds);
            }
            return created;
        }

        protected async Task<T> Delete(T entity)
        {
            var deleteOperation = TableOperation.Delete(entity);
            await Table.ExecuteAsync(deleteOperation);
            return entity;
        }

        protected async Task<bool> DropTable()
        {
            var deleted = await Table.DeleteIfExistsAsync();
            //Prevent Table operation delai
            if (deleted)
            {
                var nbretry = 5;
                while (await Table.ExistsAsync() && nbretry-- > 0) await Task.Delay(1000);
            }
            return deleted;
        }

        protected Task<IEnumerable<T>> GetPropsAsync(string[] props, string filter, CancellationToken cancellationToken = default)
        {
            var query = new TableQuery<T>();

            query = query.Select(props).Where(filter);

            return ExecuteTableQueryAsync(query, cancellationToken);
        }

        protected Task<IEnumerable<T>> GetAsync(string filter, CancellationToken cancellationToken = default)
        {
            var query = new TableQuery<T>();
            query = query.Where(filter);
            return ExecuteTableQueryAsync(query, cancellationToken);
        }

        protected Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> where, CancellationToken cancellationToken = default)
        {
            var query = Table.CreateQuery<T>().Where(where);
            return ExecuteTableQueryAsync(query.AsTableQuery(), cancellationToken);
        }

        protected async Task<T> GetByIdAsync(string partitionKey, string rowKey, string[] properties)

        {
            var props = (properties == null) ? new List<string>() : new List<string>(properties);
            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey,
                                                               rowKey,
                                                              props);

            var retrievedResult = await Table.ExecuteAsync(retrieveOperation);
            var fetchedEntity = (T)retrievedResult.Result;

            return fetchedEntity;
        }

        protected async Task<T> Insert(T entity)
        {
            var insertOperation = TableOperation.Insert(entity);
            await Table.ExecuteAsync(insertOperation);
            return entity;
        }

        protected async Task<T> Merge(T entity)
        {
            var insertOperation = TableOperation.Merge(entity);
            await Table.ExecuteAsync(insertOperation);
            return entity;
        }

        protected async Task<T> Replace(T entity)
        {
            var insertOperation = TableOperation.Replace(entity);
            await Table.ExecuteAsync(insertOperation);
            return entity;
        }

        protected async Task<IEnumerable<T>> ExecuteTableQueryAsync(TableQuery<T> tableQuery, CancellationToken cancellationToken = default)
        {
            var continuationToken = default(TableContinuationToken);
            var results = new List<T>();

            do
            {
                //Execute initial (cloudTable based query) or the next scoped query segment async.
                var queryResult = await _retryPolicy.ExecuteAsync(() => Table.ExecuteQuerySegmentedAsync(tableQuery, continuationToken, cancellationToken));
                

                //Set exact results list capacity with result count.
                results.Capacity += queryResult.Results.Count;

                //Add segment results to results list.
                results.AddRange(queryResult.Results);

                continuationToken = queryResult.ContinuationToken;

                //Continuation token is not null, more records to load.
                if (continuationToken != null && tableQuery.TakeCount.HasValue)
                {
                    //Query has a take count, calculate the remaining number of items to load.
                    var itemsToLoad = tableQuery.TakeCount.Value - results.Count;

                    //If more items to load, update query take count, or else set next query to null.
                    tableQuery = itemsToLoad > 0
                        ? tableQuery.Take(itemsToLoad)
                        : null;
                }
            } while (continuationToken != null && tableQuery != null && !cancellationToken.IsCancellationRequested);

            return results;
        }

        private static bool HandleStorageException(StorageException storageException)
        {
            var exentedInformation = storageException?.RequestInformation?.ExtendedErrorInformation;

            return exentedInformation?.ErrorCode == TableErrorCodeStrings.TableNotFound ||
             exentedInformation?.ErrorCode == TableErrorCodeStrings.TableBeingDeleted;
        }
    }
}