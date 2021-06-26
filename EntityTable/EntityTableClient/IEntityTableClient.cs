using EntityTableService.QueryExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService
{
    public interface IEntityTableClientRuntimeConfig<T>
    {
        void AddObserver(string name, IEntityObserver<T> observer);
        Task InsertOrMergeAsync(T entity);
        void RemoveObserver(string name);
    }

    public interface IEntityTableClient<T> : IEntityTableClientRuntimeConfig<T>
    {
        Task<IEnumerable<T>> GetAsync(string partition, Action<IQueryCompose<T>> filter = default, CancellationToken cancellationToken = default);

        Task InsertOrReplaceAsync(T entity);

        Task InsertOrReplaceAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task InsertOrMergeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task<T> GetByIdAsync(string partition, object id);

        Task<IEnumerable<T>> GetByAsync<P>(string partition, Expression<Func<T, P>> property, P value, Action<IQueryCompose<T>> filter = null);

        Task<IEnumerable<T>> GetByAsync(string partition, string propertyName, object value, Action<IQueryCompose<T>> filter = null);

        IAsyncEnumerable<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken= default);

        Task DeleteAsync(T entity);
    }
}