using System;
using System.Runtime.Serialization;

namespace EntityTableService.AzureClient
{
    public class BatchedTableClientException : Exception
    {
        public BatchedTableClientException()
        {
        }

        public BatchedTableClientException(string message) : base(message)
        {
        }

        public BatchedTableClientException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BatchedTableClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}