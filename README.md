# EntityStorageServices 
Entity services is an experimental project to store and manage pure Entities in Azure Storage Cloud services.

## EntityTableService 
EntityTableService is an Azure Table Storage Client based on original SDK.
The goal of this project is make more abstraction of a denormalized storage system with sufficient performance

It provide some additional features:

* Pure and strongly typed Entity per table: no longer need to inherit our entities with ITableEntity interface
* Additionnal indexes based on Entity props and ETG 
* Queryable dynamics props linked to an Entity
* Custom metadatas attached to and entity
* Lightweight query expression helper 



### EntityTableClient configuration example
```csharp
  
  static string partitionKey(string accountId) => $"Account-{accountId}";
  
  var entityClient = new EntityTableClient<PersonEntity>(
               new EntityTableClientOptions(connectionString, $"{nameof(PersonEntity)}Table", maxConcurrentInsertionTasks: 10),
               c =>
               {
                   c.SetPartitionResolver(e => partitionKey(e.AccountId));
                   c.SetPrimaryKey(p => p.PersonId);
                   c.AddIndex(p => p.Created);
                   c.AddIndex(p => p.LastName);
                   c.AddIndex(p => p.Enabled);
                   c.AddIndex(p => p.Distance);
                   c.AddIndex("_FirstLastName3Chars");
                   c.AddDynamicProp("_IsInFrance", p => (p.Address.State == "France"));
                   c.AddDynamicProp("_MoreThanOneAddress", p => (p.OtherAddress.Count > 1));
                   c.AddDynamicProp("_CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6)));
                   c.AddDynamicProp("_FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3));
                   
               }
            );

```

### Example: Query the Azure storage with entityTableClient

* Get entities with inner prop
```csharp
    //Get entities with indexed prop
    using (var mesure = counters.Mesure("Get By LastName (indexed)"))
    {
        foreach (var person in persons.Take(OPERATION_COUNT))
        {
            var result = await entityClient.GetByAsync(
                partitionKey(person.AccountId),
                p => p.LastName,
                person.LastName,
                w => w.Where(p => p.LastName).Equal(person.LastName));
        }
    }
    //Get entities with not indexed prop
    using (var mesure = counters.Mesure("Get By LastName (not indexed)"))
    {
        foreach (var person in persons.Take(OPERATION_COUNT))
        {
            var  = await entityClient.GetAsync(
                partitionKey(person.AccountId),
                w => w.Where(p => p.LastName).Equal(person.LastName));
        }
    }
```

### Sample console projet (400K entities with standard storageV2 account storage)

```
Generate faked 1000 entities...Ok
Insert 1000 entities...in 1,2617757 seconds
====================================
Get By Id :0,436 seconds
Get By LastName (indexed) :0,517 seconds
Get LastName start with 'arm'  :0,355 seconds
Get By LastName (not indexed) :9,276 seconds
Get LastName start with 'arm' (not indexed)  :7,292 seconds
====================================
```
*You should use a real azure table storage connection with more than 50K entities to have relevant results*


