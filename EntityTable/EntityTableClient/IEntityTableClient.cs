using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    public interface IEntityTableClientRuntimeConfig<T> {

        void AddObserver(string name,IEntityObserver<T> observer);
        void RemoveObserver(string name);
    }
    public interface IEntityTableClient<T>: IEntityTableClientRuntimeConfig<T>
    {
        Task<IEnumerable<T>> GetAsync(string partition, Action<IQuery<T>> query = default, CancellationToken cancellationToken = default);
        Task InsertOrReplaceAsync(T entity);
        Task InsertOrMergeAsync(T entity);
        Task InsertOrReplaceAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<T> GetByIdAsync(string partition, object id);
        Task<IEnumerable<T>> GetByAsync<P>(string partition, Expression<Func<T, P>> property, P value, Action<IQuery<T>> query = null);
        Task<IEnumerable<T>> GetByAsync(string partition, string propertyName, object value, Action<IQuery<T>> query = null);
        Task DeleteAsync(T entity);
    }
}