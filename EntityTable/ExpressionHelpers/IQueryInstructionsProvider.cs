namespace EntityTableService.ExpressionHelpers
{
    public interface IQueryInstructionsProvider : IQueryInstructions
    {
        string Get(string instruction);
    }
}