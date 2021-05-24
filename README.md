
## EntityTableService
EntityTableService is a part of EntityStorageServices, an experimental set of tools to manage your entities in Azure Storage Cloud services.
Specifically, EntityTableService help you to manage pure entities in Azure table storage. It is based on the official Azure storage SDK Client.

This project is focused on entities abstraction and performance.
 
Features:

* Pure and strongly typed Entity: no explicit dependency with ITableEntity interface
* Custom indexes 
* Indexable computed props linked to an Entity
* Custom metadatas per entity
* Lightweight and extensible query expression builder (no dependency with ITableEntity)
* Entity table observers

### How it works?

EntityTableClient generate and manage entity projections to store custom indexes.
Internally, it use Azure storage ETG feature (entity transaction group) to keep projections synchronized with the main entity.

### Sample console 
[Sample console application](https://github.com/Evodim/EntityStorageServices/blob/main/Samples/EntityClientSample.cs)

### Test project
[Tests](https://github.com/Evodim/EntityStorageServices/blob/main/EntityTableServices.Tests/EntityTableClientTests.cs)

 Remark: *Azure emulator required by default*

### EntityTableClient configuration example

```csharp
  
var options = new EntityTableClientOptions(ConnectionString, $"{nameof(PersonEntity)}Table", maxConcurrentInsertionTasks: 10);

var entityClient = EntityTableClient.CreateEntityTableClient<PersonEntity>(options, config =>
    {
        config
        //Partition key could be composed with any string based values
        .SetPartitionKey(p => p.AccountId)
        //Define an entity prop as primary key 
        .SetPrimaryKey(p => p.PersonId)

        //Add additionnal indexes
        .AddIndex(p => p.Created)
        .AddIndex(p => p.LastName)
        .AddIndex(p => p.Distance)
        .AddIndex(p => p.Enabled)
        .AddIndex(p => p.Latitude)
        .AddIndex(p => p.Longitude)

        //Add computed props, computed on each updates.
        .AddComputedProp("_IsInFrance", p => (p.Address.State == "France"))
        .AddComputedProp("_MoreThanOneAddress", p => (p.OtherAddress.Count > 1))
        .AddComputedProp("_CreatedNext6Month", p => (p.Created > DateTimeOffset.UtcNow.AddMonths(-6)))
        .AddComputedProp("_FirstLastName3Chars", p => p.LastName.ToLower().Substring(0, 3))
        //Native props values could be overrided by computed props
        .AddComputedProp(nameof(PersonEntity.FirstName), p => p.FirstName.ToUpperInvariant())
       
        //Add index for any computed props
        .AddIndex("_FirstLastName3Chars");
    });

```

### Usage example: Query the Azure storage with entityTableClient

```csharp
   //Query entities with configured primarykey
    _ = await entityClient.GetByIdAsync(
                person.AccountId,
                person.PersonId);
                

   //Query entities with any props 
    _ = await entityClient.GetAsync(
                person.AccountId,
                w => w.Where(p => p.LastName).Equal(person.LastName));

  //Query entities by indexed prop
    _ = await entityClient.GetByAsync(
                person.AccountId,
                p => p.LastName,
                person.LastName);


    //Query entities with computed prop
    _ = await entityClient.GetAsync(
                person.AccountId,
                w => w.Where("_FirstLastName3Chars").Equal("arm"));
                  
   //Query entities by indexed computed prop
   _ = await entityClient.GetByAsync(
                person.AccountId,
               "_FirstLastName3Chars", "arm");  
```

### Sample console projet (600K entities with standard storageV2 account storage)

```
Generate faked 1000 entities...Ok
Insert 1000 entities...in 2,3804459 seconds
====================================
1. Get By Id 0,048 seconds
2. Get By LastName 1,983 seconds
3. Get By LastName (indexed) 0,1 seconds
4. Get LastName start with 'arm' 2,044 seconds
5. Get by LastName start with 'arm' (indexed) 0,056 seconds
====================================
```
*You should use a real azure table storage connection with more than 50K entities to have relevant results*


