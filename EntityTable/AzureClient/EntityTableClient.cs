using EntityTable.Extensions;
using Evod.Toolkit.Azure.Storage.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Evod.Toolkit.Azure.Storage
{
    public class EntityTableClient<T> : TableStorageFacade<TableEntityBinder<T>>
    where T : class, new()
    {
        private readonly EntityTableClientConfig<T> _config;
        private readonly EntityTableClientOptions _options;

        public EntityTableClient(EntityTableClientOptions options, Action<EntityTableClientConfig<T>> configurator = null) : base(options.TableName, options.ConnectionString)
        {
            _options = options;
            _config = new EntityTableClientConfig<T>
            {
                PartitionKeyResolver = (e) => $"_{ShortHash(ResolvePrimaryKey(e))}"
            };

            configurator?.Invoke(_config);
            //override rowkey builder when primary key is setted
            _ = _config.PrimaryKey ?? throw new ArgumentNullException($"{nameof(_config.PrimaryKey)}");
        }

        protected enum BatchOperation
        {
            Insert,
            InsertOrMerge
        }

        public async Task<IEnumerable<T>> GetAsync(string partition, Action<IQuery<T>> query, CancellationToken cancellationToken = default)
        {
            IEnumerable<TableEntityBinder<T>> result;
            var queryExpr = new QueryExpression<T>();

            queryExpr
                .Where("PartitionKey").Equal(partition)
                .And(query);
            var strQuery = new TableStorageQueryBuilder<T>(queryExpr).Build();

            result = await base.GetAsync(new TableStorageQueryBuilder<T>(queryExpr).Build(), cancellationToken);
            if (result == null) return Enumerable.Empty<T>();

            return result.Select(r => r.OriginalEntity);
        }

        public async Task<IEnumerable<IDictionary<string, object>>> GetPropsAsync(string partition, string[] props, Action<IQuery<T>> query, CancellationToken cancellationToken = default)
        {
            var queryExpr = new QueryExpression<T>();

            queryExpr
                .Where("PartitionKey").Equal(partition)
                .And(query);
            var strQuery = new TableStorageQueryBuilder<T>(queryExpr).Build();

            var result = await base.GetPropsAsync(props, strQuery, cancellationToken);
            return result
                .Select(e => e.GetProperties(props)
                .ToDictionary(p => p.Key, p => p.Value));
        }

        public  Task UpdateProps(string partition, IEnumerable<IDictionary<string, object>> props, Action<IQuery<T>> query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new  NotImplementedException());
        }

        public async Task<T> GetByIdAsync(string partition, object id)
        {
            var rowKey = ComputePrimaryKey(id);

            var result = await GetByIdAsync(partition, rowKey, new string[] { });
            if (result == null) return default;

            return result.OriginalEntity;
        }

        public async Task<IEnumerable<T>> GetByIndexAsync<P>(string partition, Expression<Func<T, P>> property, P indexValue, Action<IQuery<T>> query = null)
        {
            var indexPrefix = ComputeIndexPrefix(property.GetPropertyInfo(), indexValue);
            IEnumerable<TableEntityBinder<T>> result;
            var queryExpr = new QueryExpression<T>();

            var baseQuery = queryExpr
                 .Where("PartitionKey").Equal(partition)
                 .And("RowKey").GreaterThanOrEqual(indexPrefix)
                 .And("RowKey").LessThan($"{indexPrefix}~");
            if (query != null) baseQuery = baseQuery.And(query);

            var strQuery = new TableStorageQueryBuilder<T>(queryExpr).Build();

            //if (value is DateTime || value is DateTimeOffset || value is DateTime? || value is DateTimeOffset?)
            result = await base.GetAsync(strQuery);

            if (result == null) return Enumerable.Empty<T>();

            return result.Select(r =>
            {
                return r.OriginalEntity;
            });
        }

        public async Task InsertOrReplace(T entity)
        {
            var tableEntity = CreateTableEntityBinder(entity);
            var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            batchedClient.InsertOrReplace(tableEntity);
            ApplyDynamicProps(batchedClient, tableEntity);
            ApplyIndexes(batchedClient, tableEntity);
            await batchedClient.ExecuteAsync();
        }

        public async Task InsertOrMerge(T entity)
        {
            var tableEntity = CreateTableEntityBinder(entity);
            var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            batchedClient.InsertOrMerge(tableEntity);
            ApplyDynamicProps(batchedClient, tableEntity);
            ApplyIndexes(batchedClient, tableEntity);
            await batchedClient.ExecuteAsync();
        }

        public async Task InsertOrReplace(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var blockIndex = 0;
            int pageSize = _options.MaxItemsPerInsertion;
            do
            {
                var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
                var entitiesRange = entities.Skip(blockIndex * pageSize).Take(pageSize / (_config.Indexes.Count() + 1));
                blockIndex++;
                foreach (var entity in entitiesRange)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var tableEntity = CreateTableEntityBinder(entity);
                    batchedClient.InsertOrReplace(tableEntity);
                    ApplyDynamicProps(batchedClient, tableEntity);
                    ApplyIndexes(batchedClient, tableEntity);
                }
                if (!cancellationToken.IsCancellationRequested)
                    await batchedClient.ExecuteAsync();
            }
            while (blockIndex * pageSize < entities.Count());
        }

        public string ResolvePartitionKey(T entity) => _config.PartitionKeyResolver(entity);

        public string ResolvePrimaryKey(T entity)
        {
            return ComputePrimaryKey(_config.PrimaryKey.GetValue(entity));
        }

        protected string ComputeIndexPrefix(PropertyInfo property, object value)
        {
            if (!_config.Indexes.ContainsKey(property.Name)) throw new KeyNotFoundException($"Given Index not configured: {property.Name}");
            var strValue = FormatValueToKey(value);
            return $"{ComputeKeyConvention(property.Name, strValue)}";
        }

        protected virtual string ComputeKeyConvention(string name, object value) => $"{name}-{FormatValueToKey(value)}";

        protected string ComputePrimaryKey(object value)
        {
            return $"${ComputeKeyConvention(_config.PrimaryKey.Name, value)}";
        }

        protected string ResolveIndexKey(PropertyInfo property, T entity)
        {
            var value = FormatValueToKey(property.GetValue(entity));
            return $"{ComputeKeyConvention(property.Name, value)}{ResolvePrimaryKey(entity)}";
        }

        private void ApplyDynamicProps(BatchedTableClient client, TableEntityBinder<T> tableEntity)
        {
            foreach (var prop in _config.DynamicProps)
            {
                var value = prop.Value.Invoke(tableEntity.OriginalEntity);
                tableEntity.Metadatas.Add($"_{prop.Key}", prop.Value.Invoke(tableEntity.OriginalEntity));
            }
        }

        private void ApplyIndexes(BatchedTableClient client, TableEntityBinder<T> tableEntity)
        {
            foreach (var index in _config.Indexes)
            {
                var indexedKey = ResolveIndexKey(index.Value, tableEntity.OriginalEntity);
                var indexedEntity = CreateTableEntityBinder(tableEntity.OriginalEntity, indexedKey);
                foreach (var metadata in tableEntity.Metadatas)
                {
                    indexedEntity.Metadatas.Add(metadata);
                }
                tableEntity.Metadatas.Add($"_{index.Key}Index", DateTimeOffset.UtcNow);

                client.InsertOrReplace(indexedEntity);
            }
        }

        private string FormatValueToKey(object value)
        {
            if (value is Guid guid)
            {
                return guid.ToShortGuid();
            }
            if (value is Guid?)
            {
                return ((Guid?)value).GetValueOrDefault().ToShortGuid();
            }
            if (value is DateTime time)
            {
                return time.ToString("o", CultureInfo.InvariantCulture);
            }
            if (value is DateTime?)
            {
                return ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);
            }
            if (value is DateTimeOffset timeOffset)
            {
                return timeOffset.ToString("o", CultureInfo.InvariantCulture);
            }
            if (value is DateTimeOffset?)
            {
                return ((DateTimeOffset)value).ToString("o", CultureInfo.InvariantCulture);
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private string ShortHash(string str)
        {
            var allowedSymbols = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
            var hash = new char[6];

            for (int i = 0; i < str.Length; i++)
            {
                hash[i % 6] = (char)(hash[i % 6] ^ str[i]);
            }

            for (int i = 0; i < 6; i++)
            {
                hash[i] = allowedSymbols[hash[i] % allowedSymbols.Length];
            }

            return new string(hash);
        }

        private TableEntityBinder<T> CreateTableEntityBinder(T entity, string customRowKey = null)
            => new TableEntityBinder<T>(entity, ResolvePartitionKey(entity), customRowKey ?? ResolvePrimaryKey(entity));
    }
}