using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EntityTableService
{
    public interface IQueryInstructions : IOperatorInstructions, IComparatorInstructions
    {
    }

    public interface IOperatorInstructions
    {
        string And { get; }
        string Not { get; }
        string Or { get; }
    }

    public interface IComparatorInstructions
    {
        string Equal { get; }
        string NotEqual { get; }
        string GreaterThan { get; }
        string GreaterThanOrEqual { get; }
        string LessThan { get; }
        string LessThanOrEqual { get; }
    }

    public interface IQuery<T>
    {
        IQueryFilter<T, P> AddQuery<P>(Expression<Func<T, P>> property);

        IQueryFilter<T> AddQuery(string property);
    }

    public interface IQueryOperator<T>
    {
        IQueryFilter<T, P> AddOperator<P>(string expressionOperator, Expression<Func<T, P>> property);

        IQueryFilter<T> AddOperator(string expressionOperator, string property);

        IQueryOperator<T> AddGroupExpression(string expressionOperator, Action<IQuery<T>> subQuery);
    }

    public interface IQueryFilter<T> : IQueryFilter<T, object> { }

    public interface IQueryFilter<T, P>
    {
        IQueryOperator<T> AddFilterCondition(string comparison, P value);
    }

    public interface IQueryExpression<T> : IQueryOperator<T>, IQueryFilter<T>, IQuery<T>
    {
        string PropertyName { get; set; }
        Type PropertyType { get; set; }
        object PropertyValue { get; set; }
        string Comparator { get; set; }
        string Operator { get; set; }
        List<IQueryExpression<T>> Group { get; }
        public string GroupOperator { get; set; }
        IQueryExpression<T> NextOperation { get; set; }
    }
}