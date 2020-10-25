using System;

namespace EntityTableService
{
    public interface IEntityObserver<T> : IObserver<IEntityOperationContext<T>>
    {  
    }
    
}