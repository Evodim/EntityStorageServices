using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evod.Toolkit.Azure.Storage.Abstractions
{
    public class BatchedTableClient
    {
        private const int BatchSize = 100;
        private const int MaxAttempts = 10;
        private readonly int _batchedTasks;       
        private readonly ConcurrentQueue<Tuple<ITableEntity, TableOperation>> _operations;
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _tableName;

        public BatchedTableClient(string tableName, CloudStorageAccount account, int batchedTasks)
        {
            _tableName = tableName;
            _storageAccount = account;
            _batchedTasks = batchedTasks;
            var tableReference = MakeTableReference();
            tableReference.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            _operations = new ConcurrentQueue<Tuple<ITableEntity, TableOperation>>();
        }

        private CloudTable MakeTableReference()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(_tableName);
            return tableReference;
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

        public async Task ExecuteAsync()
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

            var ops = toExecute
                .GroupBy(tuple => tuple.Item1.PartitionKey)
                .ToList();

            foreach (var op in ops)
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
                                ExecuteBatchWithRetriesAsync(tableBatchOperation).Wait();
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