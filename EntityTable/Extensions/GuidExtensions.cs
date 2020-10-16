using System;

namespace EntityTable.Extensions
{
    public static class GuidExtensions
    {
        public static string ToShortGuid(this Guid guid) => Convert.ToBase64String(guid.ToByteArray()).TrimEnd('=').Replace("/", "%");
    }
}