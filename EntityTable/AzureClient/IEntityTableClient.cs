using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    public interface IEntityTableClient<T> {
        Task<IEnumerable<T>> Get(string partition, Action<IQuery<T>> query, CancellationToken cancellationToken = default);
        Task InsertOrReplace(T entity);
        Task InsertOrMerge(T entity);
        Task InsertOrReplace(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<T> GetById(string partition, object id);
        Task<IEnumerable<T>> GetBy<P>(string partition, Expression<Func<T, P>> property, P value, Action<IQuery<T>> query = null);
        Task<IEnumerable<T>> GetBy(string partition, string propertyName, object value, Action<IQuery<T>> query = null);
        Task Delete(T entity);


    }
}