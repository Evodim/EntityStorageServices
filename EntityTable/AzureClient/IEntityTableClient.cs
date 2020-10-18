using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    public interface IEntityTableClient<T> {
        Task<IEnumerable<T>> GetAsync(string partition, Action<IQuery<T>> query, CancellationToken cancellationToken = default);
        Task InsertOrReplace(T entity);
        Task InsertOrMerge(T entity);
        Task InsertOrReplace(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<T> GetByIdAsync(string partition, object id);
        Task<IEnumerable<T>> GetByAsync<P>(string partition, Expression<Func<T, P>> property, P value, Action<IQuery<T>> query = null);
        Task<IEnumerable<T>> GetByAsync(string partition, string propertyName, object value, Action<IQuery<T>> query = null);


    }
}