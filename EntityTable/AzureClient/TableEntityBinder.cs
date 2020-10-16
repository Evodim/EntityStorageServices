using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evod.Toolkit.Azure.Storage
{
    public static class TableConstants
    {
        public const int TableServiceBatchMaximumOperations = 100;
        public const string Select = "$select";
        public const string Top = "$top";
        public const string Filter = "$filter";
        public const string TableName = "TableName";
        public const string Etag = "ETag";
        public const string Timestamp = "Timestamp";
        public const string RowKey = "RowKey";
        public const string PartitionKey = "PartitionKey";
        public const string TableServiceTablesName = "Tables";
        public const int TableServiceMaxStringPropertySizeInChars = 32768;
        public const long TableServiceMaxPayload = 20971520;
        public const int TableServiceMaxStringPropertySizeInBytes = 65536;
        public const int TableServiceMaxResults = 1000;
        public const string TableServiceNextTableName = "NextTableName";
        public const string TableServiceNextRowKey = "NextRowKey";
        public const string TableServiceNextPartitionKey = "NextPartitionKey";
        public const string TableServicePrefixForTableContinuation = "x-ms-continuation-";
        public const string UserAgentProductVersion = "1.0.6";
        public static readonly DateTimeOffset MinDateTime;
    }

    public class TableEntityBinder<T> : TableEntity
        where T : class, new()
    {
        public T OriginalEntity { get; set; }
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> Metadatas { get; } = new Dictionary<string, object>();
        protected IEnumerable<PropertyInfo> EntityProperties = typeof(T).GetProperties();

        public TableEntityBinder() : base()
        {
            OriginalEntity = new T();
        }

        public TableEntityBinder(T entity)
        {
            OriginalEntity = entity;
        }

        public TableEntityBinder(T entity, string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
            OriginalEntity = entity;
        }

        //chipped from windows azure table storage TableEntity
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            ReadEntity(this, properties, operationContext);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return WriteEntity(this, operationContext);
        }

        public void ReadEntity(ITableEntity entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            OriginalEntity = new T();
            Metadatas.Clear();
            BindEntity(entity, entity.GetType().GetProperties(), properties);
            BindEntity(OriginalEntity, EntityProperties, properties);
            BindMetadatas(Metadatas, EntityProperties, properties);
        }

        public IDictionary<string, EntityProperty> WriteEntity(ITableEntity entity, OperationContext operationContext)
        {
            Dictionary<string, EntityProperty> retVals = new Dictionary<string, EntityProperty>();

            var objectProperties = entity.GetType().GetProperties().Union(EntityProperties);
            foreach (var metadata in Metadatas)
            {
                if (retVals.ContainsKey(metadata.Key)) continue;
                retVals.Add(metadata.Key, CreateEntityPropertyFromObject(metadata.Value, true));
            }
            foreach (PropertyInfo property in objectProperties)
            {
                // reserved properties
                if (property.Name == TableConstants.PartitionKey ||
                    property.Name == TableConstants.RowKey ||
                    property.Name == TableConstants.Timestamp ||
                    property.Name == TableConstants.Etag ||
                    property.Name == "OriginalEntity")

                {
                    continue;
                }

                // Enforce public getter / setter

                if (property.GetSetMethod() == null || !property.GetSetMethod().IsPublic || property.GetGetMethod() == null || !property.GetGetMethod().IsPublic)

                {
                    continue;
                }

                EntityProperty newProperty = CreateEntityPropertyFromObject(property.GetValue(OriginalEntity, null), true);

                // property will be null if unknown type
                if (newProperty != null /* && !IsPropertyNull(newProperty)*/)
                {
                    retVals.Add(property.Name, newProperty);
                }
            }

            return retVals;
        }

        private static bool IsPropertyNull(EntityProperty prop)
        {
            switch (prop.PropertyType)
            {
                case EdmType.Binary:
                    return prop.BinaryValue == null;

                case EdmType.Boolean:
                    return !prop.BooleanValue.HasValue;

                case EdmType.DateTime:
                    return !prop.DateTimeOffsetValue.HasValue;

                case EdmType.Double:
                    return !prop.DoubleValue.HasValue;

                case EdmType.Guid:
                    return !prop.GuidValue.HasValue;

                case EdmType.Int32:
                    return !prop.Int32Value.HasValue;

                case EdmType.Int64:
                    return !prop.Int64Value.HasValue;

                case EdmType.String:
                    return prop.StringValue == null;

                default:
                    throw new InvalidOperationException("Unknown type!");
            }
        }

        private static EntityProperty CreateEntityPropertyFromObject(object value, bool allowUnknownTypes)
        {
            if (value is string)
            {
                return new EntityProperty((string)value);
            }
            else if (value is byte[])
            {
                return new EntityProperty((byte[])value);
            }
            else if (value is bool)
            {
                return new EntityProperty((bool)value);
            }
            else if (value is bool?)
            {
                return new EntityProperty((bool?)value);
            }
            else if (value is DateTime)
            {
                return new EntityProperty((DateTime)value);
            }
            else if (value is DateTime?)
            {
                return new EntityProperty((DateTime?)value);
            }
            else if (value is DateTimeOffset)
            {
                if (((DateTimeOffset)value) == default) return null;
                return new EntityProperty((DateTimeOffset?)value);
            }
            else if (value is DateTimeOffset?)
            {
                return new EntityProperty((DateTimeOffset?)value);
            }
            else if (value is double)
            {
                return new EntityProperty((double)value);
            }
            else if (value is double?)
            {
                return new EntityProperty((double?)value);
            }
            else if (value is Guid?)
            {
                return new EntityProperty((Guid?)value);
            }
            else if (value is Guid)
            {
                return new EntityProperty((Guid)value);
            }
            else if (value is int)
            {
                return new EntityProperty((int)value);
            }
            else if (value is int?)
            {
                return new EntityProperty((int?)value);
            }
            else if (value is long)
            {
                return new EntityProperty((long)value);
            }
            else if (value is long?)
            {
                return new EntityProperty((long?)value);
            }
            else if (value == null)
            {
                return new EntityProperty((string)null);
            }
            else if (allowUnknownTypes)
            {
                //json
                return new EntityProperty(JsonConvert.SerializeObject(value));
            }
            else
            {
                return null;
            }
        }

        public void BindEntity(object entity, IEnumerable<PropertyInfo> entityProperties, IDictionary<string, EntityProperty> sourceProperties)
        {
            foreach (var property in entityProperties)
            {
                // reserved properties
                if (property.Name == TableConstants.PartitionKey ||
                    property.Name == TableConstants.RowKey ||
                    property.Name == TableConstants.Timestamp ||
                    property.Name == "ETag" ||
                    property.Name == nameof(OriginalEntity)
                    )
                {
                    continue;
                }

                // Enforce public getter / setter

                if (property.GetSetMethod() == null || !property.GetSetMethod().IsPublic || property.GetGetMethod() == null || !property.GetGetMethod().IsPublic)

                {
                    continue;
                }

                // only proceed with properties that have a corresponding entry in the dictionary
                if (!sourceProperties.ContainsKey(property.Name))
                {
                    continue;
                }

                EntityProperty entityProperty = sourceProperties[property.Name];

                if (entityProperty == null)
                {
                    property.SetValue(entity, null, null);
                }
                else
                {
                    switch (entityProperty.PropertyType)
                    {
                        case EdmType.String:
                            if (property.PropertyType != typeof(string) && property.PropertyType != typeof(String))
                            {
                                //we asume the is serialized object
                                property.SetValue(entity, JsonConvert.DeserializeObject(entityProperty.StringValue, property.PropertyType), null);
                                continue;
                            }

                            property.SetValue(entity, entityProperty.StringValue, null);
                            break;

                        case EdmType.Binary:
                            if (property.PropertyType != typeof(byte[]))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.BinaryValue, null);
                            break;

                        case EdmType.Boolean:
                            if (property.PropertyType != typeof(bool) && property.PropertyType != typeof(Boolean) && property.PropertyType != typeof(Boolean?) && property.PropertyType != typeof(bool?))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.BooleanValue, null);
                            break;

                        case EdmType.DateTime:
                            if (property.PropertyType == typeof(DateTime))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue.Value.UtcDateTime, null);
                            }
                            else if (property.PropertyType == typeof(DateTime?))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue.HasValue ? entityProperty.DateTimeOffsetValue.Value.UtcDateTime : (DateTime?)null, null);
                            }
                            else if (property.PropertyType == typeof(DateTimeOffset))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue.Value, null);
                            }
                            else if (property.PropertyType == typeof(DateTimeOffset?))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue, null);
                            }

                            break;

                        case EdmType.Double:
                            if (property.PropertyType != typeof(double) && property.PropertyType != typeof(Double) && property.PropertyType != typeof(Double?) && property.PropertyType != typeof(double?))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.DoubleValue, null);
                            break;

                        case EdmType.Guid:
                            if (property.PropertyType != typeof(Guid) && property.PropertyType != typeof(Guid?))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.GuidValue, null);
                            break;

                        case EdmType.Int32:
                            if (property.PropertyType != typeof(int) && property.PropertyType != typeof(Int32) && property.PropertyType != typeof(Int32?) && property.PropertyType != typeof(int?))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.Int32Value, null);
                            break;

                        case EdmType.Int64:
                            if (property.PropertyType != typeof(long) && property.PropertyType != typeof(Int64) && property.PropertyType != typeof(long?) && property.PropertyType != typeof(Int64?))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.Int64Value, null);
                            break;
                    }
                }
            }
        }

        public void BindMetadatas(IDictionary<string, object> metadatas, IEnumerable<PropertyInfo> entityProperties, IDictionary<string, EntityProperty> sourceProperties)
        {
            foreach (var sourceProperty in sourceProperties)
            {
                //ignore entity properties
                if (entityProperties.Any(p => p.Name == sourceProperty.Key)) continue;
                metadatas.Add(sourceProperty.Key, sourceProperty.Value?.PropertyAsObject ?? null);
            }
        }

        public IEnumerable<KeyValuePair<string, object>> GetProperties(string[] properties)
        {
            return EntityProperties
                .Where(p => properties.Contains(p.Name))
                .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(OriginalEntity)))
                .Concat(Metadatas.Where(m => properties.Contains(m.Key)));
        }
    }
}