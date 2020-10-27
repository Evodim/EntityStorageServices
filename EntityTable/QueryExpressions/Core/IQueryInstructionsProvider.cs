namespace EntityTableService.QueryExpressions.Core
{
    public interface IQueryInstructionsProvider
    {
        string Get(string instruction);
    }
}