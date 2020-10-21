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
            //Default partitionKeyResolver
            _config = new EntityTableClientConfig<T>
            {
                PartitionKeyResolver = (e) => $"_{ShortHash(ResolvePrimaryKey(e))}"
            };
            configurator?.Invoke(_config);
            //PrimaryKey required
            _ = _config.PrimaryKey ?? throw new ArgumentNullException($"{nameof(_config.PrimaryKey)}");

            
        }

        public EntityTableClient(EntityTableClientOptions options, EntityTableClientConfig<T> config) : base(options.TableName, options.ConnectionString)
        {
            _options = options;
            _config = config;

            //PrimaryKey required
            _ = _config.PrimaryKey ?? throw new ArgumentNullException($"{nameof(_config.PrimaryKey)}");

            //Default partitionKeyResolver         
            if (_config.PartitionKeyResolver == null) _config.PartitionKeyResolver = (e) => $"_{ShortHash(ResolvePrimaryKey(e))}";
        }

        public async Task<IEnumerable<T>> GetAsync(string partition, Action<IQuery<T>> query = default, CancellationToken cancellationToken = default)
        {
            IEnumerable<TableEntityBinder<T>> result;
            var queryExpr = new QueryExpression<T>();

            if (query != null)
                queryExpr
                    .Where("PartitionKey").Equal(partition)
                    .And(query);
            else
                queryExpr
                .Where("PartitionKey").Equal(partition);
                

            var strQuery = new TableStorageQueryBuilder<T>(queryExpr).Build();

            result = await base.GetAsync(new TableStorageQueryBuilder<T>(queryExpr).Build(), cancellationToken);
            if (result == null) return Enumerable.Empty<T>();

            return result.Select(r => r.OriginalEntity);
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

            throw new InvalidFilterCriteriaException($"Property: {property.GetPropertyInfo().Name}, not indexed");
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

            throw new InvalidFilterCriteriaException($"Property: {propertyName}, not indexed");
        }

        public async Task InsertOrReplaceAsync(T entity)
        {
            var tableEntity = CreateTableEntityBinder(entity);
            var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            batchedClient.InsertOrReplace(tableEntity);

            ApplyDynamicProps(batchedClient, tableEntity);
            ApplyIndexes(batchedClient, tableEntity);
            await batchedClient.ExecuteAsync();
            NotifyChange(tableEntity,EntityOperation.Upsert);
        }

        public async Task InsertOrMergeAsync(T entity)
        {
            var tableEntity = CreateTableEntityBinder(entity);
            var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            batchedClient.InsertOrMerge(tableEntity);
            ApplyDynamicProps(batchedClient, tableEntity);
            ApplyIndexes(batchedClient, tableEntity);
            await batchedClient.ExecuteAsync();
            NotifyChange(tableEntity,EntityOperation.Upsert);
        }

        public async Task InsertOrReplaceAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var blockIndex = 0;
            //adapt page size with duplicated entities
            var pageSize = _options.MaxItemsPerInsertion*(1+_config.Indexes.Count());

            do
            {
                var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
                var entitiesRange = entities.Skip(blockIndex * pageSize).Take(pageSize);
                blockIndex++;
                var tableEntities = new List<TableEntityBinder<T>>();
                foreach (var entity in entitiesRange)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var tableEntity = CreateTableEntityBinder(entity);
                    batchedClient.InsertOrReplace(tableEntity);
                    ApplyDynamicProps(batchedClient, tableEntity);
                    tableEntities.Add(tableEntity);
                    ApplyIndexes(batchedClient, tableEntity);
                }

                if (!cancellationToken.IsCancellationRequested)
                    await batchedClient.ExecuteAsync();
             
                foreach (var tableEntity in tableEntities)
                {
                    NotifyChange(tableEntity,EntityOperation.Upsert);
                }
                tableEntities.Clear();
            }
            while (blockIndex * pageSize < entities.Count());
        }

        public async Task DeleteAsync(T entity)
        {
            var tableEntity = CreateTableEntityBinder(entity);
            var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);

            batchedClient.Delete(tableEntity);
            ApplyDynamicProps(batchedClient, tableEntity, deleted: true);
            ApplyIndexes(batchedClient, tableEntity, deleted: true);
            await batchedClient.ExecuteAsync();
            NotifyChange(tableEntity,EntityOperation.Delete);
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

       

        protected void NotifyChange(TableEntityBinder<T> tableEntity,EntityOperation operation)
        {
            foreach (var observer in _config.Observers)
            {
                observer.Value.OnNext(new EntityOperationContext<T>() { 
                    Entity= tableEntity.OriginalEntity,
                    Metadatas=tableEntity.Metadatas,
                    Partition=tableEntity.PartitionKey,
                    TableOperation=operation
                });
            }
        }

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

        private void ApplyDynamicProps(BatchedTableClient client, TableEntityBinder<T> tableEntity, bool deleted = false)
        {
            foreach (var prop in _config.DynamicProps)
            {
                if (deleted && tableEntity.Metadatas.ContainsKey(prop.Key))
                {
                    tableEntity.Metadatas.Remove(prop.Key);
                    continue;
                }
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

            result = await base.GetAsync(strQuery);

            if (result == null) return Enumerable.Empty<T>();

            return result.Select(r =>
            {
                return r.OriginalEntity;
            });
        }

        private void ApplyIndexes(BatchedTableClient client, TableEntityBinder<T> tableEntity, bool deleted = false)
        {
            foreach (var index in _config.Indexes)
            {
                var indexedKey = ResolveIndexKey(index.Value, tableEntity.OriginalEntity);
                var indexedEntity = CreateTableEntityBinder(tableEntity.OriginalEntity, indexedKey);
                ApplyIndex(index.Key, indexedKey, indexedEntity, client, tableEntity, deleted);
            }
            foreach (var name in _config.ComputedIndexes)
            {
                var indexedKey = ResolveIndexKey(name, tableEntity.Metadatas[$"{name}"], tableEntity.OriginalEntity);
                var indexedEntity = CreateTableEntityBinder(tableEntity.OriginalEntity, indexedKey);
                ApplyIndex(name, indexedKey, indexedEntity, client, tableEntity, deleted);
            }
            if (deleted == true) tableEntity.Metadatas.Clear();
        }

        private void ApplyIndex(string name, string indexedKey, TableEntityBinder<T> indexedEntity, BatchedTableClient client, TableEntityBinder<T> tableEntity, bool deleted)
        {
            if (deleted)
            {
                client.Delete(indexedEntity);
                return;
            }
            foreach (var metadata in tableEntity.Metadatas)
            {
                indexedEntity.Metadatas.Add(metadata);
            }
            tableEntity.Metadatas.Add($"_{name}Index", DateTimeOffset.UtcNow);

            client.InsertOrReplace(indexedEntity);
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

        public void AddObserver(string name,IEntityObserver<T> observer)
        {
            _config.Observers.Add(name,observer);
        }
        public void RemoveObserver(string name)
        {
            _config.Observers.Remove(name);
        }
    }
}