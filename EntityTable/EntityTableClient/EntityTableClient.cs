using EntityTable.Extensions;
using EntityTableService.AzureClient;
using EntityTableService.QueryExpressions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService
{

    public static class EntityTableClient { 
        public static IEntityTableClient<T> CreateEntityTableClient<T>(
                 EntityTableClientOptions options,
                 Action<EntityTableClientConfig<T>> configurator)
                   where T : class, new()
        {
            var config = new EntityTableClientConfig<T>();
            configurator?.Invoke(config);
            return new EntityTableClient<T>(options, config);
        }
    }
    /// <summary>
    /// Top level class to manage entity binded to a table
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityTableClient<T> : TableStorageService<TableEntityBinder<T>>, IEntityTableClient<T>
    where T : class, new()
    {
        protected const string DELETED = "_DELETED_";

        private readonly EntityTableClientConfig<T> _config;
        private readonly EntityTableClientOptions _options;
         
        public EntityTableClient(EntityTableClientOptions options, EntityTableClientConfig<T> config) : base(options.TableName, options.ConnectionString)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            _ = config ?? throw new ArgumentNullException(nameof(config));


            _options = options;
            _config = config;

            //PrimaryKey required
            _ = _config.PrimaryKey ?? throw new ArgumentNullException($"{nameof(_config.PrimaryKey)}");

            //Default partitionKeyResolver
            if (_config.PartitionKeyResolver == null) _config.PartitionKeyResolver = (e) => $"_{ShortHash(ResolvePrimaryKey(e))}";
        }

        public async Task<IEnumerable<T>> GetAsync(string partition, Action<IQueryCompose<T>> filter = default, CancellationToken cancellationToken = default)
        {
            IEnumerable<TableEntityBinder<T>> result;
            var queryExpr = new FilterExpression<T>();

            if (filter != null)
                queryExpr
                    .Where("PartitionKey").Equal(partition)
                    .And(filter);
            else
                queryExpr
                .Where("PartitionKey").Equal(partition);

            var strQuery = new TableStorageQueryBuilder<T>(queryExpr).Build();

            try
            {
                result = await GetAsync(strQuery, cancellationToken);
                if (result == null) return Enumerable.Empty<T>();
                return result.Select(r => r.Entity);
            }
            catch (Exception ex)
            {
                throw new EntityTableClientException($"{EntityTableClientExceptionMessages.UnableToGetEntity}", partition, strQuery, ex);
            }
        }

        public async Task<T> GetByIdAsync(string partition, object id)
        {
            var rowKey = ComputePrimaryKey(id);
            try
            {
                var result = await GetByIdAsync(partition, rowKey, new string[] { });
                if (result == null) return default;
                return result.Entity;
            }
            catch (Exception ex)
            {
                throw new EntityTableClientException($"{EntityTableClientExceptionMessages.UnableToGetEntity}", partition, rowKey, ex);
            }
        }

        public async Task<IEnumerable<T>> GetByAsync<P>(string partition, Expression<Func<T, P>> property, P value, Action<IQueryCompose<T>> filter = null)
        {
            if (!_config.Indexes.ContainsKey(property.GetPropertyInfo().Name))
            {
                throw new EntityTableClientException($"Property: {property.GetPropertyInfo().Name}, not indexed");
            }

            var propertyKey = ComputeIndexPrefix(property.GetPropertyInfo(), value);
            try
            {
                return await GetByPropAsync(partition, propertyKey, filter);
            }
            catch (Exception ex)
            {
                throw new EntityTableClientException($"{EntityTableClientExceptionMessages.UnableToGetEntity}", partition, propertyKey, ex);
            }
        }

        public async Task<IEnumerable<T>> GetByAsync(string partition, string propertyName, object value, Action<IQueryCompose<T>> filter = null)
        {
            if (_config.ComputedIndexes.Contains(propertyName) || _config.Indexes.ContainsKey(propertyName))
            {
                var propertyKey = "";
                try
                {
                    propertyKey = ComputeIndexPrefix(propertyName, value);
                    return await GetByPropAsync(partition, propertyKey, filter);
                }
                catch (Exception ex)
                {
                    throw new EntityTableClientException($"{EntityTableClientExceptionMessages.UnableToGetEntity}", partition, propertyKey, ex);
                }
            }

            throw new EntityTableClientException($"Property: {propertyName}, not indexed");
        }

        public Task InsertOrReplaceAsync(T entity)
        {
            return UpdateEntity(entity, EntityOperation.Replace);
        }

        public Task InsertOrMergeAsync(T entity)
        {
            return UpdateEntity(entity, EntityOperation.Merge);
        }

        public async Task InsertMany(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                var blockIndex = 0;
                //adapt page size with duplicated entities
                var pageSize = _options.MaxItemsPerInsertion * (1 + _config.Indexes.Count());

                do
                {
                    var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
                    var cleaner = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
                    var entitiesRange = entities.Skip(blockIndex * pageSize).Take(pageSize);
                    blockIndex++;
                    var tableEntities = new List<TableEntityBinder<T>>();
                    foreach (var entity in entitiesRange)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var tableEntity = CreateTableEntityBinder(entity);

                        //initial metada required to be not filtered
                        tableEntity.Metadatas.Add(DELETED, false);

                        batchedClient.InsertOrReplace(tableEntity);
                        ApplyDynamicProps(tableEntity);
                        tableEntities.Add(tableEntity);
                        ApplyIndexes(batchedClient, cleaner, tableEntity);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                        await batchedClient.ExecuteParallelAsync();

                    foreach (var tableEntity in tableEntities)
                    {
                        NotifyChange(tableEntity, EntityOperation.Replace);
                    }
                    tableEntities.Clear();
                }
                while (blockIndex * pageSize < entities.Count());
            }
            catch (Exception ex)
            {
                throw new EntityTableClientException(EntityTableClientExceptionMessages.UnableToUpsertEntity, ex);
            }
        }

        public async Task DeleteAsync(T entity)
        {
            var tableEntity = CreateTableEntityBinder(entity);
            var batchedClient = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            try
            {
                //get index rowkeys
                var metadatas = await GetEntityMetadatasAsync(tableEntity.PartitionKey, tableEntity.RowKey);

                //mark index deleted
                batchedClient.Delete(tableEntity);
                foreach (var index in metadatas.Where(m => m.Key.EndsWith("Index_")))
                {
                    var tableEntityIndex = CreateTableEntityBinder(entity, index.Value.ToString());
                    batchedClient.Delete(tableEntityIndex);
                }
                await batchedClient.ExecuteAsync();
                NotifyChange(tableEntity, EntityOperation.Delete);
            }
            catch (Exception ex)
            {
                throw new EntityTableClientException($"{EntityTableClientExceptionMessages.UnableToDeleteEntity}", tableEntity?.PartitionKey, tableEntity?.RowKey, ex);
            }
        }

        public async Task<IDictionary<string, object>> GetEntityMetadatasAsync(string partitionKey, string rowKey)
        {
            var metadataKeys = _config.Indexes.Keys.Select(k => $"_{k}Index_").ToList();
            metadataKeys.AddRange(_config.ComputedIndexes.Select(k => $"_{k}Index_").ToList());
            try
            {
                var entity = await GetByIdAsync(partitionKey, rowKey, metadataKeys.ToArray());
                return entity?.Metadatas ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                throw new EntityTableClientException($"{EntityTableClientExceptionMessages.UnableToGetEntity}, partition:{partitionKey} rowkey:{rowKey}", ex);
            }
        }

        public string ResolvePartitionKey(T entity) => _config.PartitionKeyResolver(entity);

        public string ResolvePrimaryKey(T entity)
        {
            return ComputePrimaryKey(_config.PrimaryKey.GetValue(entity));
        }

        public void AddObserver(string name, IEntityObserver<T> observer)
        {
            _config.Observers.TryAdd(name, observer);
        }

        public void RemoveObserver(string name)
        {
            _config.Observers.TryRemove(name, out var _);
        }

        protected enum BatchOperation
        {
            Insert,
            InsertOrMerge
        }

        protected virtual string ComputeKeyConvention(string name, object value) => $"{name}-{FormatValueToKey(value)}";

        protected void NotifyChange(TableEntityBinder<T> tableEntity, EntityOperation operation)
        {
            foreach (var observer in _config.Observers)
            {
                observer.Value.OnNext(new EntityOperationContext<T>()
                {
                    Entity = tableEntity.Entity,
                    Metadatas = tableEntity.Metadatas,
                    Partition = tableEntity.PartitionKey,
                    TableOperation = operation
                });
            }
        }

        protected void NotifyError(Exception exception)
        {
            foreach (var observer in _config.Observers)
            {
                observer.Value.OnError(exception);
            }
        } 
         
        private async Task UpdateEntity(T entity, EntityOperation operation)
        {
            var client = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            var cleaner = CreateBatchedClient(_options.MaxBatchedInsertionTasks);
            //get existing entity
            var tableEntity = CreateTableEntityBinder(entity);
            try
            {
                //get index rowkeys
                var metadatas = await GetEntityMetadatasAsync(tableEntity.PartitionKey, tableEntity.RowKey);

                //initial metada required to be not filtered
                tableEntity.Metadatas.Add(DELETED, false);
                //mark index deleted
                ApplyDynamicProps(tableEntity);
                ApplyIndexes(client, cleaner, tableEntity, metadatas);

                if (operation == EntityOperation.Replace)
                {
                    client.InsertOrReplace(tableEntity);
                }
                if (operation == EntityOperation.Merge)
                {
                    client.InsertOrMerge(tableEntity);
                }

                await client.ExecuteAsync();
                NotifyChange(tableEntity, operation);
                await cleaner.ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw new EntityTableClientException($"{EntityTableClientExceptionMessages.UnableToUpsertEntity}, partition:{tableEntity.PartitionKey} rowkey:{tableEntity.RowKey}", ex);
            }
        }

        private async Task<IEnumerable<T>> GetByPropAsync(string partition, string indexPrefix, Action<IQueryCompose<T>> query = null)
        {
            IEnumerable<TableEntityBinder<T>> result;
            var queryExpr = new FilterExpression<T>();

            var baseQuery = queryExpr
                 .Where("PartitionKey").Equal(partition)
                 .And("RowKey").GreaterThanOrEqual(indexPrefix)
                 .And("RowKey").LessThan($"{indexPrefix}~")
                 .And(DELETED).Equal(false);
            if (query != null) baseQuery.And(query);

            var strQuery = new TableStorageQueryBuilder<T>(queryExpr).Build();

            result = await base.GetAsync(strQuery);

            if (result == null) return Enumerable.Empty<T>();

            return result.Select(r =>
            {
                return r.Entity;
            });
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

        private string CreateRowKey(PropertyInfo property, T entity)
        {
            var value = FormatValueToKey(property.GetValue(entity));
            return $"{ComputeKeyConvention(property.Name, value)}{ResolvePrimaryKey(entity)}";
        }

        private string CreateRowKey(string key, object value, T entity)
        {
            var strValue = FormatValueToKey(value);
            return $"{ComputeKeyConvention(key, strValue)}{ResolvePrimaryKey(entity)}";
        }

        private void ApplyDynamicProps(TableEntityBinder<T> tableEntity, bool toDelete = false)
        {
            foreach (var prop in _config.DynamicProps)
            {
                if (toDelete && tableEntity.Metadatas.ContainsKey(prop.Key))
                {
                    tableEntity.Metadatas.Remove(prop.Key);
                    continue;
                }

                tableEntity.Metadatas.Add(prop.Key, prop.Value.Invoke(tableEntity.Entity));
            }
        }

        private void ApplyIndexes(BatchedTableClient client, BatchedTableClient cleaner, TableEntityBinder<T> tableEntity, IDictionary<string, object> existingMetadatas = null)
        {
            var metadaDataIndexes = new Dictionary<string, object>();
            foreach (var index in _config.Indexes)
            {
                var indexedKey = CreateRowKey(index.Value, tableEntity.Entity);
                var indexedEntity = CreateTableEntityBinder(tableEntity.Entity, indexedKey);
                ApplyIndex(indexedEntity, tableEntity);
                client.InsertOrReplace(indexedEntity);
                metadaDataIndexes.Add($"_{index.Key}Index_", indexedKey);
            }
            foreach (var name in _config.ComputedIndexes)
            {
                var indexedKey = CreateRowKey(name, tableEntity.Metadatas[$"{name}"], tableEntity.Entity);
                var indexedEntity = CreateTableEntityBinder(tableEntity.Entity, indexedKey);
                ApplyIndex(indexedEntity, tableEntity);
                client.InsertOrReplace(indexedEntity);
                metadaDataIndexes.Add($"_{name}Index_", indexedKey);
            }
            if (existingMetadatas != null)
            {
                var metadatasToDelete = existingMetadatas.Where(m => !metadaDataIndexes.ContainsValue(m.Value) && m.Key.EndsWith("Index_"));
                //cleanup old indexes
                foreach (var metadata in metadatasToDelete)
                {
                    var indexedKey = metadata.Value.ToString();
                    var indexedEntityToDelete = CreateTableEntityBinder(tableEntity.Entity, indexedKey);
                    //logical delete, remove it when transaction commited
                    indexedEntityToDelete.Metadatas.Add(DELETED, true);
                    client.InsertOrReplace(indexedEntityToDelete);
                    cleaner.Delete(indexedEntityToDelete);
                }
            }

            //attach indexes keys to main entity
            foreach (var metadataIdx in metadaDataIndexes)
                tableEntity.Metadatas.Add(metadataIdx);
        }

        private static void ApplyIndex(TableEntityBinder<T> indexedEntity, TableEntityBinder<T> tableEntity)
        {
            foreach (var metadata in tableEntity.Metadatas)
            {
                indexedEntity.Metadatas.Add(metadata);
            }
        }

        private static string FormatValueToKey(object value)
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

        private static string ShortHash(string str)
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