using EntityTable.Extensions;
using System;
using System.Linq.Expressions;

namespace Evod.Toolkit.Azure.Storage
{
    public static class EntityTableClientConfigExtensions
    {
        public static EntityTableClientConfig<T> SetPartitionResolver<T>(this EntityTableClientConfig<T> config, Func<T, string> resolver)
        {
            config.PartitionKeyResolver = resolver;
            return config;
        }

        public static EntityTableClientConfig<T> SetPrimaryKey<T, P>(this EntityTableClientConfig<T> config, Expression<Func<T, P>> selector)
        {
            var property = selector.GetPropertyInfo();
            config.PrimaryKey = property;
            return config;
        }

        public static EntityTableClientConfig<T> AddIndex<T, P>(this EntityTableClientConfig<T> config, Expression<Func<T, P>> selector)
        {
            var property = selector.GetPropertyInfo();

            config.Indexes.Add(property.Name, property);
            return config;
        }

        public static EntityTableClientConfig<T> AddDynamicProp<T>(this EntityTableClientConfig<T> config, string propName, Func<T, object> propValue)
        {
            config.DynamicProps.Add(propName, propValue);
            return config;
        }
    }
}