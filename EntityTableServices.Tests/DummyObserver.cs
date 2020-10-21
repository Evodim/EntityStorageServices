using EntityTableService.AzureClient;
using EntityTableService.Tests.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EntityTableService.Tests
{
    public class DummyObserver : IEntityObserver<PersonEntity>
    {
        private long _created=0;
        private long _deleted=0;
        public long CreatedCount => _created;
        public long DeletedCount => _deleted;

        public ConcurrentDictionary<string, PersonEntity> Persons= new ConcurrentDictionary<string, PersonEntity>();

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(IEntityOperationContext<PersonEntity> operation)
        {
            if (operation.TableOperation == EntityOperation.Upsert)
            {
                Persons.TryAdd(operation.Partition + operation.Entity.PersonId, operation.Entity);
                Interlocked.Increment(ref _created);
            }
            if (operation.TableOperation == EntityOperation.Delete)
            {

                Persons.Remove(operation.Partition + operation.Entity.PersonId, out var deleted);
                Interlocked.Increment(ref _deleted);
            }


        }
 
    }
}