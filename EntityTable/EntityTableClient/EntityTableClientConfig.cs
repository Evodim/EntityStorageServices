using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EntityTableService
{
    public class EntityTableClientConfig<T>
    {
        public Func<T, string> PartitionKeyResolver { get; set; }
        public Dictionary<string, Func<T, object>> DynamicProps = new Dictionary<string, Func<T, object>>();
        public List<string> ComputedIndexes = new List<string>();
        public Dictionary<string, PropertyInfo> Indexes = new Dictionary<string, PropertyInfo>();        
        public Dictionary<string, IEntityObserver<T>> Observers = new Dictionary<string, IEntityObserver<T>>();
        public PropertyInfo PrimaryKey { get; set; }

    }
   
}