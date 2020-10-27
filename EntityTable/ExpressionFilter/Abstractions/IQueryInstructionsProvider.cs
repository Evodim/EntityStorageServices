namespace EntityTableService.ExpressionFilter.Abstractions
{
    public interface IQueryInstructionsProvider
    {
        string Get(string instruction);
    }
}