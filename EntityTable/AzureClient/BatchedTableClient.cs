using EntityTableService.Extensions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    public class BatchedTableClient
    {
        private readonly int _batchSize;
        private readonly int _maxAttempts;
        private readonly int _waitAndRetrySeconds;
        private readonly int _batchedTasks;
        private readonly CloudTable _tableReference;
        private readonly ConcurrentQueue<Tuple<ITableEntity, TableOperation>> _operations;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;

        public BatchedTableClient(string tableName,
            CloudStorageAccount account,
            int batchedTasks = 1,
            int maxAttempts = 10,
            int batchSize = 100,
            int waitAndRetrySeconds = 1,
            bool autoCreateTable = false
            )
        {
            _tableName = tableName;
            _storageAccount = account;
            _batchedTasks = batchedTasks;
            _tableReference = MakeTableReference();
            _operations = new ConcurrentQueue<Tuple<ITableEntity, TableOperation>>();

            _retryPolicy = (autoCreateTable) ?

                Policy.Handle<StorageException>(e => e.HandleStorageException())
                .WaitAndRetryAsync(maxAttempts,
                i => TimeSpan.FromSeconds(_waitAndRetrySeconds),
                async (a, t) => await CreateTableIfNotExistsAsync()) :

                Policy.Handle<StorageException>(e => e.HandleStorageException())
                .WaitAndRetryAsync(maxAttempts,
                i => TimeSpan.FromSeconds(_waitAndRetrySeconds));

            _batchSize = batchSize;
            _maxAttempts = maxAttempts;
            _waitAndRetrySeconds = waitAndRetrySeconds;
        }

        private CloudTable MakeTableReference()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(_tableName);
            return tableReference;
        }

        public async Task CreateTableIfNotExistsAsync()
        {
            var created = await _tableReference.CreateIfNotExistsAsync();

            //Prevent internal Table operation delay
            if (created)
            {
                var nbretry = _maxAttempts;
                while (!await _tableReference.ExistsAsync() && nbretry-- > 0) await Task.Delay(_waitAndRetrySeconds);
            }
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
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                ExecuteBatchWithRetriesAsync(tableBatchOperation).GetAwaiter().GetResult();
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

        public Task ExecuteAsync()
        {
            //empty batch
            if (_operations.Count == 0)
                return Task.CompletedTask;

            var tableBatchOperation = MakeBatchOperation(_operations);

            return ExecuteBatchWithRetriesAsync(tableBatchOperation);
        }

        private IEnumerable<Tuple<ITableEntity, TableOperation>> GetOperations(
           IEnumerable<Tuple<ITableEntity, TableOperation>> operations,
           int batch)
        {
            return operations
                .Skip(batch * _batchSize)
                .Take(_batchSize);
        }

        private Task ExecuteBatchWithRetriesAsync(TableBatchOperation tableBatchOperation)
        {
            var tableRequestOptions = MakeTableRequestOptions();

            var tableReference = MakeTableReference();

            return _retryPolicy.ExecuteAsync(() => tableReference.ExecuteBatchAsync(tableBatchOperation, tableRequestOptions, new OperationContext()));
        }

        private TableRequestOptions MakeTableRequestOptions()
        {
            return new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(1000), _maxAttempts)
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
       
    }
}