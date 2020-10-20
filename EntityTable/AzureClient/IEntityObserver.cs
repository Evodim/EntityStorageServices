using System;
using System.Collections.Generic;

namespace EntityTableService.AzureClient
{
     
    public interface IEntityObserver<T> : IObserver<T>
    {
        void OnUpdated(string partition, T entity, IDictionary<string, object> metadatas); 
        void OnDeleted(string partition, T entity, IDictionary<string, object> metadatas);
       
    }
    
}