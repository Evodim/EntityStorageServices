﻿using System.Collections.Generic;

namespace EntityTableService
{
    internal class EntityOperationContext<T> : IEntityOperationContext<T>
    {
        public EntityOperation TableOperation { get; set; }
        public string Partition { get; set; }
        public T Entity { get; set; }
        public IDictionary<string, object> Metadatas { get; set; } = new Dictionary<string, object>();
    }
}