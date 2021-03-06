﻿using EntityTableService.QueryExpressions.Core;
using System.Text;

namespace EntityTableService.QueryExpressions
{
    //Base class to build a string filter from given IFilterExpression
    public abstract class BaseQueryExpressionBuilder<T>
    {
        public IFilterExpression<T> Query { get; }
        protected IQueryInstructionsProvider InstructionsProvider { get; }

        protected BaseQueryExpressionBuilder(IFilterExpression<T> query, IQueryInstructionsProvider instructionsProvider)
        {
            Query = query;
            InstructionsProvider = instructionsProvider;
        }

        protected virtual string Build(IFilterExpression<T> expression)
        {
            if (expression == null) return string.Empty;
            StringBuilder queryBuilder = new StringBuilder();
           if (expression.PropertyType != null)
            {
                var strExpression = ExpressionFilterConverter(expression);
                queryBuilder.Append(strExpression);
            }
            if (expression.Group.Count > 0)
            {
                foreach (var operation in expression.Group)
                {
                    if (!string.IsNullOrEmpty(InstructionsProvider.Get(operation.GroupOperator))) queryBuilder.Append($" {InstructionsProvider.Get(operation.GroupOperator)} (");
                    queryBuilder.Append(Build(operation));
                    if (!string.IsNullOrEmpty(InstructionsProvider.Get(operation.GroupOperator))) queryBuilder.Append(")");
                }
            }
            if (!string.IsNullOrEmpty(expression.Operator))
                queryBuilder.Append($" {InstructionsProvider.Get(expression.Operator)} ");
            queryBuilder.Append(Build(expression.NextOperation));

            return queryBuilder.ToString().Trim();
        }

        public string Build()
        {
            return Build(Query);
        }

        protected virtual string ExpressionFilterConverter(IFilterExpression<T> expression)
        {
            return $"{expression.PropertyName} {InstructionsProvider.Get(expression.Comparator)} '{expression.PropertyValue}'";
        }
    }
}