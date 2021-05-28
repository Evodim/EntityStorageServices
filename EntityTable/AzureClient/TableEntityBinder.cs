using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace EntityTableService.AzureClient
{
    public class TableEntityBinder<T> : TableEntity
        where T : class, new()
    {
        public T Entity { get; set; }
        public readonly IDictionary<string, object> Properties = new Dictionary<string, object>();
        public readonly IDictionary<string, object> Metadatas = new Dictionary<string, object>();
        protected readonly IEnumerable<PropertyInfo> EntityProperties = typeof(T).GetProperties();

        public TableEntityBinder() : base()
        {
            Entity = new T();
        }

        public TableEntityBinder(T entity)
        {
            Entity = entity;
        }
        public TableEntityBinder(string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
            Entity = default;
        }
        public TableEntityBinder(T entity , string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
            Entity = entity;
        }
        
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            ReadEntity(this, properties);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return WriteEntity(this);
        }

        public void ReadEntity(ITableEntity entity, IDictionary<string, EntityProperty> properties)
        {

            Entity = new T();
            Metadatas.Clear();
            ReadProp(entity, entity.GetType().GetProperties(), properties);
            ReadProp(Entity, EntityProperties, properties);
            ReadMetadatas(Metadatas, EntityProperties, properties);
        }

        public IDictionary<string, EntityProperty> WriteEntity(ITableEntity entity)
        {
            Dictionary<string, EntityProperty> retVals = new Dictionary<string, EntityProperty>();

            var objectProperties = entity.GetType().GetProperties().Union(EntityProperties);
            foreach (var metadata in Metadatas)
            {
                if (retVals.ContainsKey(metadata.Key)) continue;
                retVals.Add(metadata.Key, CreateEntityPropertyFromObject(metadata.Value));
            }
            foreach (PropertyInfo property in objectProperties)
            {
                // reserved properties
                if (property.Name == TableConstants.PartitionKey ||
                    property.Name == TableConstants.RowKey ||
                    property.Name == TableConstants.Timestamp ||
                    property.Name == TableConstants.Etag ||
                    property.Name == nameof(Entity))

                {
                    continue;
                }

                // Enforce public getter / setter

                if (property.GetSetMethod() == null || !property.GetSetMethod().IsPublic || property.GetGetMethod() == null || !property.GetGetMethod().IsPublic)

                {
                    continue;
                }

                var newProperty = CreateEntityPropertyFromObject(property.GetValue(Entity, null), property);

                // property will be null for unknown type
                if (newProperty != null && !retVals.ContainsKey(property.Name))
                { 
                   retVals.Add(property.Name, newProperty);
               }
            }

            return retVals;
        }

        private static EntityProperty CreateEntityPropertyFromObject(object value, PropertyInfo propertyInfo = null, bool handleComplexProp = true)
        {
            var propertyType = propertyInfo?.PropertyType ?? typeof(object);
             
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propertyType = propertyType.GetGenericArguments().First();
            }

            if (value is string @string)
            {
                return new EntityProperty(@string);
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
                return new EntityProperty((DateTimeOffset)value);
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
            //handle additional value types
            else if (value is decimal)
            {
                return new EntityProperty(((decimal)value).ToString(CultureInfo.InvariantCulture));
            }
            else if (value is decimal?)
            {
                return new EntityProperty(((decimal?)value)?.ToString(CultureInfo.InvariantCulture));
            }
            else if (value is float)
            {
                return new EntityProperty(((float)value).ToString(CultureInfo.InvariantCulture));
            }
            else if (value is float?)
            {
                return new EntityProperty(((float?)value)?.ToString(CultureInfo.InvariantCulture));
            }
            else if (propertyType.IsEnum)
            {
                return new EntityProperty(value.ToString());
            }
            else if (handleComplexProp)
            {
                //json
                return new EntityProperty(JsonConvert.SerializeObject((value)));
            }
            else
            {
                return null;
            }
        }

        public void ReadProp(object entity, IEnumerable<PropertyInfo> entityProperties, IDictionary<string, EntityProperty> sourceProperties)
        {
            foreach (var property in entityProperties)
            {
                // reserved properties
                if (property.Name == TableConstants.PartitionKey ||
                    property.Name == TableConstants.RowKey ||
                    property.Name == TableConstants.Timestamp ||
                    property.Name == "ETag" ||
                    property.Name == nameof(Entity)
                    )
                {
                    continue;
                }

                // Enforce public getter / setter
                if (property.GetSetMethod() == null ||
                    !property.GetSetMethod().IsPublic ||
                    property.GetGetMethod() == null ||
                    !property.GetGetMethod().IsPublic)
                {
                    continue;
                }
                var propertyType = property.PropertyType;

                //Handle nullable types globally
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = property.PropertyType.GetGenericArguments().First();
                }
                //Ignore other incompatible types

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
                            if (propertyType == typeof(decimal) ||
                                propertyType == typeof(Decimal))
                            {
                                {
                                    if (decimal.TryParse(entityProperty.StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                                    {
                                        property.SetValue(entity, value, null);
                                        continue;
                                    }
                                    //unable de pase value ignore it
                                    continue;
                                }
                            }
                            if (propertyType == typeof(float) ||
                               propertyType == typeof(Single))
                            {
                                {
                                    if (float.TryParse(entityProperty.StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                                    {
                                        property.SetValue(entity, value, null);
                                        continue;
                                    }
                                    //unable de pase value ignore it
                                    continue;
                                }
                            }
                            if (propertyType.IsEnum)
                            {
                                {
                                    if (Enum.TryParse(propertyType, entityProperty.StringValue, out var parsedEnum))
                                    {
                                        property.SetValue(entity, parsedEnum, null);
                                        continue;
                                    }
                                    //unable de pase value ignore it
                                    continue;
                                }
                            }
                            if (propertyType != typeof(string) &&
                                propertyType != typeof(String) &&
                                propertyType.IsClass &&
                                !propertyType.IsValueType)
                            {
                                //otherwise  it should be a serialized object
                                property.SetValue(entity, JsonConvert.DeserializeObject(entityProperty.StringValue, propertyType), null);
                                continue;
                            }

                            property.SetValue(entity, entityProperty.StringValue, null);
                            break;

                        case EdmType.Binary:
                            if (propertyType != typeof(byte[]))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.BinaryValue, null);
                            break;

                        case EdmType.Boolean:
                            if (propertyType != typeof(bool) &&
                                propertyType != typeof(Boolean))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.BooleanValue, null);
                            break;

                        case EdmType.DateTime:
                            if (propertyType == typeof(DateTime))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue.Value.UtcDateTime, null);
                            }
                            else if (propertyType == typeof(DateTime?))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue.HasValue ? entityProperty.DateTimeOffsetValue.Value.UtcDateTime : (DateTime?)null, null);
                            }
                            else if (propertyType == typeof(DateTimeOffset))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue.Value, null);
                            }
                            else if (propertyType == typeof(DateTimeOffset?))
                            {
                                property.SetValue(entity, entityProperty.DateTimeOffsetValue, null);
                            }

                            break;

                        case EdmType.Double:
                            if (propertyType != typeof(double) &&
                                propertyType != typeof(Double))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.DoubleValue, null);
                            break;

                        case EdmType.Guid:
                            if (propertyType != typeof(Guid))
                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.GuidValue, null);
                            break;

                        case EdmType.Int32:
                            if (propertyType != typeof(int) &&
                                propertyType != typeof(Int32))

                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.Int32Value, null);
                            break;

                        case EdmType.Int64:
                            if (propertyType != typeof(long) &&
                                propertyType != typeof(Int64))

                            {
                                continue;
                            }

                            property.SetValue(entity, entityProperty.Int64Value, null);
                            break;
                    }
                }
            }
        }

        public void ReadMetadatas(IDictionary<string, object> metadatas, IEnumerable<PropertyInfo> entityProperties, IDictionary<string, EntityProperty> sourceProperties)
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
                .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(Entity)))
                .Concat(Metadatas.Where(m => properties.Contains(m.Key)));
        }
    }
}