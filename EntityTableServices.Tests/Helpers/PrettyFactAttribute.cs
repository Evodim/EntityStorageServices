using System;
using System.Linq;
using Xunit;

namespace EntityTableService.Tests.Helpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PrettyFactAttribute : FactAttribute
    {
        public PrettyFactAttribute() { }
        protected virtual Func<string, string> DisplayPrettify => (displayName) =>  string.Join("", displayName.Split("_").SelectMany(d=> d.Select(c => (char.IsUpper(c)) ? $" {char.ToLowerInvariant(c)}" : $"{c}")).ToList());
        public new string DisplayName { get { return base.DisplayName; } set { base.DisplayName = DisplayPrettify(value); } }
    }
}