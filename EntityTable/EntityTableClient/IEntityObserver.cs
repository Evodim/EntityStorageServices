using System;

namespace EntityTableService.AzureClient
{
    public interface IEntityObserver<T> : IObserver<IEntityOperationContext<T>>
    {
        
       
    }
    
}