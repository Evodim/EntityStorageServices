using System;
using System.Linq.Expressions;

namespace Evod.Toolkit.Azure.Storage
{
    public static class QueryExpressionExtensions
    {
        public static IQueryFilter<T, P> Where<T, P>(this IQuery<T> query, Expression<Func<T, P>> property)
        {
            return query.AddQuery(property);
        }

        public static IQueryFilter<T> Where<T>(this IQuery<T> query, string property)
        {
            return query.AddQuery(property);
        }

        public static IQueryOperator<T> Equal<T, P>(this IQueryFilter<T, P> query, P value)
        {
            return query.AddFilterCondition(nameof(IQueryInstructions.Equal), value);
        }

        public static IQueryOperator<T> NotEqual<T, P>(this IQueryFilter<T, P> query, P value)
        {
            return query.AddFilterCondition(nameof(IQueryInstructions.NotEqual), value);
        }

        public static IQueryOperator<T> GreaterThan<T, P>(this IQueryFilter<T, P> query, P value)
        {
            return query.AddFilterCondition(nameof(IQueryInstructions.GreaterThan), value);
        }

        public static IQueryOperator<T> GreaterThanOrEqual<T, P>(this IQueryFilter<T, P> query, P value)
        {
            return query.AddFilterCondition(nameof(IQueryInstructions.GreaterThanOrEqual), value);
        }

        public static IQueryOperator<T> LessThan<T, P>(this IQueryFilter<T, P> query, P value)
        {
            return query.AddFilterCondition(nameof(IQueryInstructions.LessThan), value);
        }

        public static IQueryOperator<T> LessThanOrEqual<T, P>(this IQueryFilter<T, P> query, P value)
        {
            return query.AddFilterCondition(nameof(IQueryInstructions.LessThanOrEqual), value);
        }

        public static IQueryFilter<T, P> And<T, P>(this IQueryOperator<T> query, Expression<Func<T, P>> property)
        {
            return query.AddOperator(nameof(IQueryInstructions.And), property);
        }

        public static IQueryFilter<T> And<T>(this IQueryOperator<T> query, string property)
        {
            return query.AddOperator(nameof(IQueryInstructions.And), property);
        }

        public static IQueryFilter<T> Not<T>(this IQueryOperator<T> query, string property)
        {
            return query.AddOperator(nameof(IQueryInstructions.Not), property);
        }

        public static IQueryFilter<T> Or<T>(this IQueryOperator<T> query, string property)
        {
            return query.AddOperator(nameof(IQueryInstructions.Or), property);
        }

        public static IQueryFilter<T, P> Not<T, P>(this IQueryOperator<T> query, Expression<Func<T, P>> property)
        {
            return query.AddOperator(nameof(IQueryInstructions.Not), property);
        }

        public static IQueryFilter<T, P> Or<T, P>(this IQueryOperator<T> query, Expression<Func<T, P>> property)
        {
            return query.AddOperator(nameof(IQueryInstructions.Or), property);
        }

        public static IQueryOperator<T> And<T>(this IQueryOperator<T> query, Action<IQuery<T>> subQuery)
        {
            return query.AddGroupExpression(nameof(IQueryInstructions.And), subQuery);
        }

        public static IQueryOperator<T> Or<T>(this IQueryOperator<T> query, Action<IQuery<T>> subQuery)
        {
            return query.AddGroupExpression(nameof(IQueryInstructions.Or), subQuery);
        }
    }
}