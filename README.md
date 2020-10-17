# EntityStorageServices 
Entity services is an experimental project to store and manage pure Entities in Azure Storage Cloud services.

## EntityTableService 
EntityTableService is an Azure Table Storage Client based on original SDK.
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

### EntityTableClient Get example
```csharp
var EnabledPersons = await entityClient.GetAsync(
  partitionKey(firstPerson.AccountId), w => w.Where(p => p.Enabled).Equal(false)
  );
```


and more to come...





