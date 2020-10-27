using EntityTableService.ExpressionFilter.Abstractions;

namespace EntityTableService.ExpressionFilter
{
    public class TableStorageInstructions : InstructionsProviderBase, IQueryInstructions
    {
        public string And => "and";

        public string Not => "not";

        public string Or => "or";

        public string Equal => "eq";

        public string NotEqual => "ne";

        public string GreaterThan => "gt";

        public string GreaterThanOrEqual => "ge";

        public string LessThan => "lt";

        public string LessThanOrEqual => "le";
    }
}