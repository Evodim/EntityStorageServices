﻿using System.Collections.Generic;

namespace EntityTableService
{
    public interface IEntityOperationContext<T>
    {
        EntityOperation TableOperation { get; set; }
        string Partition { get; set; }
        T Entity { get; set; }
        IDictionary<string, object> Metadatas { get; set; }
    }
}