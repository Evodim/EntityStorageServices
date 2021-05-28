using EntityTable.Extensions;
using System;
using System.Linq.Expressions;

namespace EntityTableService
{
    public static class EntityTableClientConfigExtensions
    {
        public static EntityTableConfig<T> SetPartitionKey<T>(this EntityTableConfig<T> config, Func<T, string> partitionKeyResolver)
        {
            config.PartitionKeyResolver = partitionKeyResolver;
            return config;
        }

        public static EntityTableConfig<T> SetPrimaryKey<T, P>(this EntityTableConfig<T> config, Expression<Func<T, P>> propertySelector)
        {
            var property = propertySelector.GetPropertyInfo();
            config.PrimaryKey = property;
            return config;
        }

        public static EntityTableConfig<T> AddIndex<T, P>(this EntityTableConfig<T> config, Expression<Func<T, P>> propertySelector)
        {
            var property = propertySelector.GetPropertyInfo();

            config.Indexes.Add(property.Name, property);
            return config;
        }

        public static EntityTableConfig<T> AddIndex<T>(this EntityTableConfig<T> config, string propName)
        {
            config.ComputedIndexes.Add(propName);
            return config;
        }

        public static EntityTableConfig<T> AddComputedProp<T>(this EntityTableConfig<T> config, string propName, Func<T, object> propValue)
        {
            config.ComputedProps.Add(propName, propValue);
            return config;
        }

        public static EntityTableConfig<T> AddObserver<T>(this EntityTableConfig<T> config, string observerName, IEntityObserver<T> entityObserver)
        {
            config.Observers.TryAdd(observerName, entityObserver);
            return config;
        }

        public static EntityTableConfig<T> RemoveObserver<T>(this EntityTableConfig<T> config, string observerName)
        {
            config.Observers.TryRemove(observerName, out var _);
            return config;
        }
    }
}