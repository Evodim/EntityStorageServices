namespace Evod.Toolkit.Azure.Storage
{
    public interface IQueryInstructionsProvider : IQueryInstructions
    {
        string Get(string instruction);
    }
}