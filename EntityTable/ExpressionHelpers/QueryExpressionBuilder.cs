using System.Text;

namespace Evod.Toolkit.Azure.Storage
{
    public class QueryExpressionBuilder<T>
    {
        public IQueryExpression<T> Query { get; }
        protected IQueryInstructionsProvider InstructionsProvider { get; }

        public QueryExpressionBuilder(IQueryExpression<T> query, IQueryInstructionsProvider instructionsProvider)
        {
            Query = query;
            InstructionsProvider = instructionsProvider;
        }

        protected virtual string Build(IQueryExpression<T> expression)
        {
            if (expression == null) return string.Empty;
            StringBuilder queryBuilder = new StringBuilder();
            if (expression.PropertyValue != null)
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

        protected virtual string ExpressionFilterConverter(IQueryExpression<T> expression)
        {
            return $"{expression.PropertyName} {InstructionsProvider.Get(expression.Comparator)} '{expression.PropertyValue.ToString()}'";
        }
    }
}