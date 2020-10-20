using EntityTableService.AzureClient;
using EntityTableService.Tests.Models;
using System;
using System.Collections.Generic;

namespace Samples
{

    public partial class EntityClientSample
    {
        public class DummyObserver : IEntityObserver<PersonEntity>
        {
            public void OnCompleted()
            {
             
            } 
            public void OnError(Exception error)
            {
               
            }
            public void OnNext(PersonEntity value)
            {
             
            }
            public void OnUpdated(string partition, PersonEntity entity,IDictionary<string,object> metadatas)
            {
               
            }
            public void OnDeleted(string partition, PersonEntity entity, IDictionary<string, object> metadatas)
            {

            }
        }
    }
}