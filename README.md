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
     new EntityTableClientOptions(connectionString, "TestTable", maxConcurrentInsertionTasks: 10),
     c =>
     {
         c.SetPartitionResolver(e => partitionKey(e.AccountId));
         c.SetPrimaryKey(p => p.PersonId);
         c.AddIndex(p => p.Created);
         c.AddIndex(p => p.LastName);
         c.AddIndex(p => p.Enabled);
         c.AddIndex(p => p.Distance);
         c.AddDynamicProp("IsInFrance", p => (p.Address.State == "France"));
         c.AddDynamicProp("MoreThanOneAddress", p => (p.OtherAddress.Count > 1));
         c.AddDynamicProp("CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6)));
         c.AddDynamicProp("FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3));
     }
  );            

```

### Example: Query the Azure storage with entityTableClient

* Get entities with inner prop
```csharp
var EnabledPersons = await entityClient.GetAsync(
  partitionKey(firstPerson.AccountId), w => w.Where(p => p.Enabled).Equal(false)
  );
```
* Get entities with dynamic prop
```csharp
  var personsWhoStartedWithArm = await entityClient.GetAsync(partitionKey(person.AccountId),
                            w => w.Where("_FirstLastName3Chars").Equal("arm"));
  );
```


and more to come...

### Sample console projet 


```
Generate faked 1000 entities...Ok
Insert 1000 entities...in 0,8004938 seconds
====================================
Get LastName start with 'arm' (dynamic props) :6,194 seconds
Get By Id :0,498 seconds
Get By LastName (not indexed) :6,595 seconds
Get By LastName (indexed) :0,569 seconds
====================================
```
*You should use a real azure table storage connection with more than 50K entities to have relevant results*


