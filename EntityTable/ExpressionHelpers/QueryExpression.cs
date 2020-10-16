using EntityTable.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Evod.Toolkit.Azure.Storage
{
    public class QueryExpression<T, P> : QueryExpression<T>, IQueryFilter<T, P>
    {
        public IQueryOperator<T> AddFilterCondition(string comparison, P value) => base.AddFilterCondition(comparison, value);
    }

    public class QueryExpression<T> : IQueryExpression<T>
    {
        public string PropertyName { get; set; }
        public Type PropertyType { get; set; }
        public string Comparator { get; set; }
        public string Operator { get; set; }
        public string GroupOperator { get; set; }
        public object PropertyValue { get; set; }
        public List<IQueryExpression<T>> Group { get; } = new List<IQueryExpression<T>>();

        public IQueryExpression<T> NextOperation { get; set; }

        public IQueryFilter<T, P> AddOperator<P>(string expressionOperator, Expression<Func<T, P>> property)
        {
            Operator = expressionOperator;
            var prop = property.GetPropertyInfo() ?? throw new InvalidFilterCriteriaException();
            var newOperation = new QueryExpression<T, P>()
            {
                PropertyName = prop.Name,
                PropertyType = prop.PropertyType
            };
            NextOperation = newOperation;
            return newOperation;
        }

        public IQueryFilter<T> AddOperator(string expressionOperator, string property)
        {
            Operator = expressionOperator;
            var newOperation = new QueryExpression<T>()
            {
                PropertyName = property,
                PropertyType = typeof(object)
            };
            NextOperation = newOperation;
            return newOperation;
        }

        public IQueryOperator<T> AddFilterCondition(string comparison, object value)
        {
            PropertyValue = value;
            Comparator = comparison;
            return this;
        }

        public IQueryOperator<T> AddGroupExpression(string expressionOperator, Action<IQuery<T>> subQuery)
        {
            var childExpression = new QueryExpression<T>();
            subQuery.Invoke(childExpression);

            childExpression.GroupOperator = expressionOperator;
            Group.Add(childExpression as IQueryExpression<T>);

            return this;
        }

        public IQueryFilter<T> AddQuery(string property)
        {
            return AddOperator(null, property);
        }

        public IQueryFilter<T, P> AddQuery<P>(Expression<Func<T, P>> property)
        {
            return AddOperator<P>(null, property);
        }
    }
}