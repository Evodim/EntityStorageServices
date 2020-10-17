using Microsoft.Azure.Cosmos.Table;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTableService.AzureClient
{
    /// <summary>
    ///   Cloud Table extension to read all rows of a table sequentialy (bypass azure client table limitation)
    /// </summary>
    public static class CloudTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query, CancellationToken cancellationToken = default, Action<IList<T>> onProgress = null)
            where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync(query, token);
                token = seg.ContinuationToken;
                items.AddRange(seg);

                if (items.Count >= query.TakeCount) break;
                onProgress?.Invoke(items);
            } while (token != null && !cancellationToken.IsCancellationRequested);

            return items;
        }
    }
}