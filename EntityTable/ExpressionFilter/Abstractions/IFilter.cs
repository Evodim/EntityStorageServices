using EntityTableService.ExpressionFilter.Abstractions;
using System;
using System.Linq.Expressions;

namespace EntityTableService
{
    public interface IFilter<T>
    {
        IQueryFilter<T, P> AddQuery<P>(Expression<Func<T, P>> property);

        IQueryFilter<T> AddQuery(string property);
    }
}