namespace Evod.Toolkit.Azure.Storage
{
    public abstract class InstructionsProviderBase
    {
        public virtual string Get(string instruction)
        {
            if (instruction == null) return string.Empty;
            var type = this.GetType();
            var value = type.GetProperty(instruction).GetValue(this) as string;
            return value;
        }
    }
}