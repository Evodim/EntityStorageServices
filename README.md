# EntityStorageServices 
Entity storage services is an experimental set of tools to manage your entities in Azure Storage Cloud services.

## EntityTableClient
EntityTableClient is an Azure table storage client based on the official SDK.

### Why?
The goal of this project is to help you to manage large entities in tables with sufficient performance and more abstraction.

Features:

* Pure and strongly typed Entity: no longer need to inherit our entities with ITableEntity interface
* Custom indexes 
* Indexable dynamics props linked to an Entity
* Custom metadatas per entity
* Lightweight query expression helper 
* Entity table observer (wip)

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
  
  var entityClient = new EntityTableClient<PersonEntity>(
               new EntityTableClientOptions(_options,
               c =>
               {
                   c.SetPartitionKey(p => p.AccountId);
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


    //Query entities with dynamic prop
    _ = await entityClient.GetAsync(
                person.AccountId,
                w => w.Where("_FirstLastName3Chars").Equal("arm"));
                  
   //Query entities by indexed dynamic prop
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


