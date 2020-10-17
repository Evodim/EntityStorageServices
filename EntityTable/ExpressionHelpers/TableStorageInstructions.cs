namespace EntityTableService.ExpressionHelpers
{
    public class TableStorageInstructions : InstructionsProviderBase, IQueryInstructionsProvider
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