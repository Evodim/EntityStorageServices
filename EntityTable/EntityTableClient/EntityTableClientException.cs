using System;
using System.Runtime.Serialization;

namespace EntityTableService

{
    [Serializable]
    public class EntityTableClientException : Exception
    {
    
        public EntityTableClientException()
        {
        }
        public EntityTableClientException(string message, string partitionKey, string query, Exception innerException) : base($"{message},partition:{partitionKey},row or query:{query}",innerException)
        {
        }
        public EntityTableClientException(string message) : base(message)
        {
        }

        public EntityTableClientException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EntityTableClientException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

      
    }
}