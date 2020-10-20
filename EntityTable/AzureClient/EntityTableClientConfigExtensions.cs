using EntityTable.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace EntityTableService.AzureClient
{
    public static partial class EntityTableClientConfigExtensions
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
        public static EntityTableClientConfig<T> AddProjection<T, P>(this EntityTableClientConfig<T> config, string projectionName,
            Func<T, P> onCreate, Action<T, P> onUpdate, Action<T, P> onDelete)
        {
            var proj = new Projection<T, P>();
            proj.OnCreate = onCreate;
            proj.OnUpdate = onUpdate;
            proj.OnDelete = onDelete;

            config.Projections.Add(projectionName, proj);
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

        public static IProjection<T> GetProjection<T>(this EntityTableClientConfig<T> config, string projectionName)
        {
            return config.Projections[projectionName] ;
        }
    }


   
       public interface IProjection<T> {
       string Name { get; set; }
       Type Type { get;  }
       Type ProjectionType { get;  }
        void Update(T entity, dynamic projection);

       }
    //var method = test.GetType().GetMethod("Update");
    //method.Invoke(test,new object[] { new PersonEntity(), new PersonDashboard() });
    public class Projection<T,P> : IProjection<T>
        {
            public string Name { get; set; }
            public Type Type => typeof(T);
            public Type ProjectionType => typeof(P);
            public Func<T, P> OnCreate { get; set; }
            public Action<T, P> OnUpdate { get; set; }
            public Action<T, P> OnDelete { get; set; }
            public void Update(T entity, dynamic projection)
            {
            OnUpdate(entity,projection);
            }
    }
}