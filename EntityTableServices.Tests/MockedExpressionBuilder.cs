using EntityTableService.QueryExpressions;

namespace EntityTableService.Tests
{
    public class MockedExpressionBuilder<T> : BaseQueryExpressionBuilder<T>
    {
        public MockedExpressionBuilder() : base(new FilterExpression<T>(), new DefaultInstructionsProvider())
        {
        }
    }
}