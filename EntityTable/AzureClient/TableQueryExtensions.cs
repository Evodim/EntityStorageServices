namespace Evod.Toolkit.Azure.Storage.AzureClient
{
    //public static class TableQueryExtensions
    //{
    //    public async static Task<IEnumerable<TElement>> ExecuteAsync<TElement>(this TableQuery<TElement> tableQuery, CancellationToken ct)
    //    {
    //        var nextQuery = tableQuery;
    //        var continuationToken = default(TableContinuationToken);
    //        var results = new List<TElement>();

    //        do
    //        {
    //            //Execute the next query segment async.
    //            var queryResult = await nextQuery.ExecuteSegmentedAsync(continuationToken, ct);

    //            //Set exact results list capacity with result count.
    //            results.Capacity += queryResult.Results.Count;

    //            //Add segment results to results list.
    //            results.AddRange(queryResult.Results);

    //            continuationToken = queryResult.ContinuationToken;

    //            //Continuation token is not null, more records to load.
    //            if (continuationToken != null && tableQuery.TakeCount.HasValue)
    //            {
    //                //Query has a take count, calculate the remaining number of items to load.
    //                var itemsToLoad = tableQuery.TakeCount.Value - results.Count;

    //                //If more items to load, update query take count, or else set next query to null.
    //                nextQuery = itemsToLoad > 0
    //                    ? tableQuery.Take(itemsToLoad)
    //                    : null;
    //            }

    //        } while (continuationToken != null && nextQuery != null && !ct.IsCancellationRequested);

    //        return results;
    //    }

    //}
}