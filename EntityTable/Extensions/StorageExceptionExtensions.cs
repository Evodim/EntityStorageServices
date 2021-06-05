using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;

namespace EntityTableService.Extensions
{
    internal static class StorageExceptionExtensions
    {
        internal static bool HandleStorageException(this StorageException storageException)
        {
            var exentedInformation = storageException?.RequestInformation?.ExtendedErrorInformation;
            return exentedInformation?.ErrorCode == TableErrorCodeStrings.TableNotFound ||
             exentedInformation?.ErrorCode == TableErrorCodeStrings.TableBeingDeleted;
        }
    }
}