using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace EntityTableService.Tests.Helpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PrettyFactAttribute : FactAttribute
    {
        public PrettyFactAttribute([CallerMemberName] string caller = null) {
            DisplayName= Prettify(caller); 
        }
        protected virtual string Prettify(string displayName) => 
            string.Join("", 
                displayName.Split("_")
                .SelectMany(word => $" {word.ToLowerInvariant()}") 
                .ToList()
                );
        public new string DisplayName { get; }
    }
}