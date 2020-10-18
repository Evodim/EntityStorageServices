using EntityTable.Extensions;
using EntityTableService.ExpressionHelpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    public class EntityTableClient<T> : TableStorageFacade<TableEntityBinder<T>>, IEntityTableClient<T>
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

        public async Task<T> GetByIdAsync(string partition, object id)
        {
            var rowKey = ComputePrimaryKey(id);

            var result = await GetByIdAsync(partition, rowKey, new string[] { });
            if (result == null) return default;

            return result.OriginalEntity;
        }

        public async Task<IEnumerable<T>> GetByAsync<P>(string partition, Expression<Func<T, P>> property, P value, Action<IQuery<T>> query = null)
        {
            if (_config.Indexes.ContainsKey(property.GetPropertyInfo().Name))
            {
                return await GetByIndexAsync(partition, property, value);
            }

            throw new InvalidFilterCriteriaException("Property not indexed");
        }

        public async Task<IEnumerable<T>> GetByAsync(string partition, string propertyName, object value, Action<IQuery<T>> query = null)
        {
            if (_config.ComputedIndexes.Contains(propertyName))
            {
                return await GetByIndexAsync(partition, propertyName, value);
            }
            if (_config.Indexes.ContainsKey(propertyName))
            {
                return await GetByIndexAsync(partition, propertyName, value);
            }

            throw new InvalidFilterCriteriaException("Property not indexed");
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

        public Task InsertOrMerge(T entity)
        {
            var tableEntity = CreateTableEntityBinder(entity);
            var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            batchedClient.InsertOrMerge(tableEntity);
            ApplyDynamicProps(batchedClient, tableEntity);
            ApplyIndexes(batchedClient, tableEntity);
            return batchedClient.ExecuteAsync();
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

        protected enum BatchOperation
        {
            Insert,
            InsertOrMerge
        }

        protected virtual string ComputeKeyConvention(string name, object value) => $"{name}-{FormatValueToKey(value)}";

        private Task<IEnumerable<T>> GetByIndexAsync(string partition, string propertyName, object indexValue, Action<IQuery<T>> query = null)
        {
            var indexPrefix = ComputeIndexPrefix(propertyName, indexValue);
            return GetByPropAsync(partition, indexPrefix, query);
        }

        private Task<IEnumerable<T>> GetByIndexAsync<P>(string partition, Expression<Func<T, P>> propertySelector, P indexValue, Action<IQuery<T>> query = null)
        {
            var indexPrefix = ComputeIndexPrefix(propertySelector.GetPropertyInfo(), indexValue);
            return GetByPropAsync(partition, indexPrefix, query);
        }

        private string ComputeIndexPrefix(PropertyInfo property, object value)
        {
            return ComputeIndexPrefix(property.Name, value);
        }

        private string ComputeIndexPrefix(string propertyName, object value)
        {
            if (!_config.ComputedIndexes.Contains(propertyName) && !_config.Indexes.ContainsKey(propertyName)) throw new KeyNotFoundException($"Given Index not configured: {propertyName}");
            var strValue = FormatValueToKey(value);
            return $"{ComputeKeyConvention(propertyName, strValue)}";
        }        

        private string ComputePrimaryKey(object value)
        {
            return $"${ComputeKeyConvention(_config.PrimaryKey.Name, value)}";
        }

        private string ResolveIndexKey(PropertyInfo property, T entity)
        {
            var value = FormatValueToKey(property.GetValue(entity));
            return $"{ComputeKeyConvention(property.Name, value)}{ResolvePrimaryKey(entity)}";
        }

        private string ResolveIndexKey(string key, object value, T entity)
        {
            var strValue = FormatValueToKey(value);
            return $"{ComputeKeyConvention(key, strValue)}{ResolvePrimaryKey(entity)}";
        }

        private void ApplyDynamicProps(BatchedTableClient client, TableEntityBinder<T> tableEntity)
        {
            foreach (var prop in _config.DynamicProps)
            {
                var value = prop.Value.Invoke(tableEntity.OriginalEntity);
                tableEntity.Metadatas.Add(prop.Key, prop.Value.Invoke(tableEntity.OriginalEntity));
            }
        }

        private async Task<IEnumerable<T>> GetByPropAsync(string partition, string indexPrefix, Action<IQuery<T>> query = null)
        {
            IEnumerable<TableEntityBinder<T>> result;
            var queryExpr = new QueryExpression<T>();

            var baseQuery = queryExpr
                 .Where("PartitionKey").Equal(partition)
                 .And("RowKey").GreaterThanOrEqual(indexPrefix)
                 .And("RowKey").LessThan($"{indexPrefix}~");
            if (query != null) baseQuery = baseQuery.And(query);

            var strQuery = new TableStorageQueryBuilder<T>(queryExpr).Build();

            //if (value is DateTime || value is DateTimeOffset || value is DateTime? || value is DateTimeOffset?)
            result = await GetAsync(strQuery);

            if (result == null) return Enumerable.Empty<T>();

            return result.Select(r =>
            {
                return r.OriginalEntity;
            });
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
            foreach (var indexKey in _config.ComputedIndexes)
            {
                var indexedKey = ResolveIndexKey(indexKey, tableEntity.Metadatas[$"{indexKey}"], tableEntity.OriginalEntity);
                var indexedEntity = CreateTableEntityBinder(tableEntity.OriginalEntity, indexedKey);
                foreach (var metadata in tableEntity.Metadatas)
                {
                    indexedEntity.Metadatas.Add(metadata);
                }
                tableEntity.Metadatas.Add($"_{indexKey}Index", DateTimeOffset.UtcNow);

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