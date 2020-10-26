namespace EntityTableService.ExpressionHelpers
{
    public class QueryExpression<T, P> : FilterExpression<T>, IQueryFilter<T, P>
    {
        public IFilterOperator<T> AddFilterCondition(string comparison, P value) => base.AddFilterCondition(comparison, value);
    }
}