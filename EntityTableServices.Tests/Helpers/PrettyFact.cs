using System;
using System.Linq;
using Xunit;

namespace EntityTableService.Tests.Helpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PrettyFact : FactAttribute
    {
        protected virtual Func<string, string> DisplayPrettify => (displayName) => string.Join("", displayName.Select(c => (char.IsUpper(c)) ? $" {char.ToLowerInvariant(c)}" : $"{c}"));
        public new string DisplayName { get { return base.DisplayName; } set { base.DisplayName = DisplayPrettify(value); } }
    }
}