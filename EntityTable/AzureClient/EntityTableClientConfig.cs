using System;
using System.Collections.Generic;
using System.Reflection;

namespace EntityTableService.AzureClient
{
    public class EntityTableClientConfig<T>
    {
        public Func<T, string> PartitionKeyResolver { get; set; }
        public Dictionary<string, Func<T, object>> DynamicProps = new Dictionary<string, Func<T, object>>();
        public Dictionary<string, PropertyInfo> Indexes = new Dictionary<string, PropertyInfo>();
        public PropertyInfo PrimaryKey { get; set; }
    }
}