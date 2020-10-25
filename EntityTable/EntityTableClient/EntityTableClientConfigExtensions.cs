using EntityTable.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace EntityTableService
{
    public static partial class EntityTableClientConfigExtensions
    {

        public static EntityTableClientConfig<T> ComposePartitionKey<T>(this EntityTableClientConfig<T> config, Func<T, string> partitionKeyResolver)
        {
            config.PartitionKeyResolver = partitionKeyResolver;
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
        public static EntityTableClientConfig<T> AddIndex<T>(this EntityTableClientConfig<T> config, string propName)
        {
            config.ComputedIndexes.Add(propName);
            return config;
        }

        public static EntityTableClientConfig<T> AddDynamicProp<T>(this EntityTableClientConfig<T> config, string propName, Func<T, object> propValue)
        {
            config.DynamicProps.Add(propName, propValue);
            return config;
        }        

        public static EntityTableClientConfig<T> AddObserver<T>(this EntityTableClientConfig<T> config,string observerName, IEntityObserver<T> entityObserver)
        {
            config.Observers.Add(observerName, entityObserver);
            return config;
        }

        public static EntityTableClientConfig<T> RemoveObserver<T>(this EntityTableClientConfig<T> config, string observerName)
        {
            config.Observers.Remove(observerName);
            return config;
        }
         
    }
}