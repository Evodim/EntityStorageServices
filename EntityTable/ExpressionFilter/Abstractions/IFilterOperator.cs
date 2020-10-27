using System;
using System.Linq.Expressions;

namespace EntityTableService.ExpressionFilter.Abstractions
{
    public interface IFilterOperator<T>
    {
        IQueryFilter<T, P> AddOperator<P>(string expressionOperator, Expression<Func<T, P>> property);

        IQueryFilter<T> AddOperator(string expressionOperator, string property);

        IFilterOperator<T> AddGroupExpression(string expressionOperator, Action<IFilter<T>> subQuery);
    }
}