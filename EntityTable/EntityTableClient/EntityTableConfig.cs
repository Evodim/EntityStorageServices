using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace EntityTableService
{
    public class EntityTableConfig<T>
    {
        public Func<T, string> PartitionKeyResolver { get; set; }
        public Dictionary<string, Func<T, object>> ComputedProps { get; } = new Dictionary<string, Func<T, object>>();
        public List<string> ComputedIndexes { get; } = new List<string>();
        public Dictionary<string, PropertyInfo> Indexes { get; } = new Dictionary<string, PropertyInfo>();
        public ConcurrentDictionary<string, IEntityObserver<T>> Observers { get; } = new ConcurrentDictionary<string, IEntityObserver<T>>();
        public PropertyInfo PrimaryKey { get; set; }
    }
}