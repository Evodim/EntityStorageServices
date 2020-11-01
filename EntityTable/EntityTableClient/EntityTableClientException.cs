using System;
using System.Runtime.Serialization;

namespace EntityTableService

{
    [Serializable]
    public class EntityTableClientException : Exception
    {
        private Exception ex;

        public EntityTableClientException()
        {
        }

        public EntityTableClientException(Exception ex)
        {
            this.ex = ex;
        }

        public EntityTableClientException(string message) : base(message)
        {
        }

        public EntityTableClientException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EntityTableClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

      
    }
}