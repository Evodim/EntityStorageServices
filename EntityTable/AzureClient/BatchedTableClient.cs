using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    public class BatchedTableClient
    {
        private const int BatchSize = 100;
        private const int MaxAttempts = 10;
        private readonly int _batchedTasks;
        private readonly CloudTable _tableReference;
        private readonly ConcurrentQueue<Tuple<ITableEntity, TableOperation>> _operations;
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;

        public BatchedTableClient(string tableName, CloudStorageAccount account, int batchedTasks)
        {
            _tableName = tableName;
            _storageAccount = account;
            _batchedTasks = batchedTasks;
            _tableReference = MakeTableReference();
            _operations = new ConcurrentQueue<Tuple<ITableEntity, TableOperation>>();
        }

        private CloudTable MakeTableReference()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(_tableName);
            return tableReference;
        }

        public Task CreateTableIfNotExists()
        {
            return _tableReference.CreateIfNotExistsAsync();
        }

        public decimal OutstandingOperations => _operations.Count;

        public void Insert<TEntity>(TEntity entity)
            where TEntity : ITableEntity
        {
            var e = new Tuple<ITableEntity, TableOperation>(entity, TableOperation.Insert(entity));
            _operations.Enqueue(e);
        }

        public void Delete<TEntity>(TEntity entity)
            where TEntity : ITableEntity
        {
            if (string.IsNullOrEmpty(entity.ETag)) entity.ETag = "*";
            var e = new Tuple<ITableEntity, TableOperation>(entity, TableOperation.Delete(entity));
            _operations.Enqueue(e);
        }

        public void InsertOrMerge<TEntity>(TEntity entity)
            where TEntity : ITableEntity
        {
            var e = new Tuple<ITableEntity, TableOperation>(entity, TableOperation.InsertOrMerge(entity));
            _operations.Enqueue(e);
        }

        public void InsertOrReplace<TEntity>(TEntity entity)
            where TEntity : ITableEntity
        {
            var e = new Tuple<ITableEntity, TableOperation>(entity, TableOperation.InsertOrReplace(entity));
            _operations.Enqueue(e);
        }

        public void Merge<TEntity>(TEntity entity)
            where TEntity : ITableEntity
        {
            var e = new Tuple<ITableEntity, TableOperation>(entity, TableOperation.Merge(entity));
            _operations.Enqueue(e);
        }

        public void Replace<TEntity>(TEntity entity)
            where TEntity : ITableEntity
        {
            var e = new Tuple<ITableEntity, TableOperation>(entity, TableOperation.Replace(entity));
            _operations.Enqueue(e);
        }

        public async Task ExecuteParallelAsync()
        {
            using var sem = new SemaphoreSlim(_batchedTasks, _batchedTasks);
            List<Task> batchTasks = new List<Task>();

            var count = _operations.Count;
            var toExecute = new List<Tuple<ITableEntity, TableOperation>>();
            for (var index = 0; index < count; index++)
            {
                _operations.TryDequeue(out var operation);
                if (operation != null)
                    toExecute.Add(operation);
            }

            foreach (var op in toExecute.GroupBy(tuple => tuple.Item1.PartitionKey))
            {
                var operations = op;
                var batch = 0;
                var operationBatch = GetOperations(operations, batch);
                while (operationBatch.Any())
                {
                    sem.Wait();
                    var tableBatchOperation = MakeBatchOperation(operationBatch);
                    batchTasks.Add(
                        Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                ExecuteBatchWithRetriesAsync(tableBatchOperation).Wait();
                            }
                            catch (AggregateException ex)
                            {

                                var storageException = ex.InnerExceptions.FirstOrDefault(s => s is StorageException);

                                if (storageException != null)
                                {
                                    await HandleStorageException(storageException as StorageException, tableBatchOperation);
                                }
                                
                            }
                            catch (StorageException ex)
                            {
                                await HandleStorageException(ex,tableBatchOperation);
                            }
                          
                            finally
                            {
                                sem.Release();
                            }
                        }));
                    batch++;
                    operationBatch = GetOperations(operations, batch);
                }
            }
            await Task.WhenAll(batchTasks);
        }
       
        
        private async Task<bool> HandleStorageException(StorageException storageException, TableBatchOperation tableBatchOperation)
        {
                var exentedInformation = storageException?.RequestInformation?.ExtendedErrorInformation;
                if (storageException?.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                //TODO handle concurrency action
                return true;
            }
                if (storageException?.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound &&
                    (exentedInformation?.ErrorCode == TableErrorCodeStrings.TableNotFound))
                {
                    //Table not exits, try to create it
                    await CreateTableIfNotExists();
                    await ExecuteBatchWithRetriesAsync(tableBatchOperation);
                    return true;
                }
                if (storageException?.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict &&
                   (exentedInformation?.ErrorCode == TableErrorCodeStrings.TableBeingDeleted))
                   {
                //Table not exits, try to create it
                await CreateTableIfNotExists();
                await ExecuteBatchWithRetriesAsync(tableBatchOperation);
               }
               


                throw new BatchedTableClientException(exentedInformation?.ErrorCode ?? "UnhandledException", storageException);
        }
        public Task ExecuteAsync()
        {
            //empty batch
            if (_operations.Count == 0)
                return Task.CompletedTask;

            var tableBatchOperation = MakeBatchOperation(_operations);
            try
            {
             return ExecuteBatchWithRetriesAsync(tableBatchOperation);
            }
            catch(StorageException ex) {
                return HandleStorageException(ex, tableBatchOperation);
            }
            
        }

        private Task ExecuteBatchWithRetriesAsync(TableBatchOperation tableBatchOperation)
        {
            var tableRequestOptions = MakeTableRequestOptions();

            var tableReference = MakeTableReference();

            return tableReference.ExecuteBatchAsync(tableBatchOperation, tableRequestOptions, new OperationContext());
        }

        private static TableRequestOptions MakeTableRequestOptions()
        {
            return new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(500), MaxAttempts)
            };
        }

        private static TableBatchOperation MakeBatchOperation(
            IEnumerable<Tuple<ITableEntity, TableOperation>> operationsToExecute)
        {
            var tableBatchOperation = new TableBatchOperation();
            foreach (var tuple in operationsToExecute)
            {
                tableBatchOperation.Add(tuple.Item2);
            }

            return tableBatchOperation;
        }

        private static IEnumerable<Tuple<ITableEntity, TableOperation>> GetOperations(
            IEnumerable<Tuple<ITableEntity, TableOperation>> operations,
            int batch)
        {
            return operations
                .Skip(batch * BatchSize)
                .Take(BatchSize);
        }
    }
}