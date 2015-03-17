# Orleans.Storage.Couchbase
   Orleans Storage Provider Of Couchbase
##simple
  You can a simple from this link [https://github.com/weitaolee/Orleans.EventSourcing](https://github.com/weitaolee/Orleans.EventSourcing "Simple Link")

##Install From Nuget
    Install-Package Orleans.Storage.Couchbase

####update log 

######2015-3-17
   1.update Read/Write/Delete to AsyncMethod
######2015-3-16
   1.fix bug   
   2.add UseGuidAsStorageKey="True/Flase" config,default is True. 

    True :store grain with a guid key
    Flase:store grian with a key like 
           GrainReference=40011c8c7bcc4141b3569464533a06a203ffffff9c20d2b7
 

## USE CASE

 
###### 1. App.config
```xml  
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <configSections>
    <sectionGroup name="couchbaseClients">
      <section name="couchbaseDataStore" type="Couchbase.Configuration.Client.Providers.CouchbaseClientSection, Couchbase.NetClient" />
    </sectionGroup>
  </configSections>
  <couchbaseClients>
    <couchbaseDataStore>
      <servers>
        <add uri="http://192.168.0.100:8091" />
      </servers>
      <buckets>
        <add name="datastore" password="datastore" useSsl="false" />
      </buckets>
    </couchbaseDataStore>
  </couchbaseClients>
  <runtime>
    <gcServer enabled="true"/>
  </runtime>
</configuration>
``` 

###### 2. ServerConfiguration.xml
```xml
<?xml version="1.0" encoding="utf-8"?>
<OrleansConfiguration xmlns="urn:orleans">
  <Globals>
    <StorageProviders>
      <Provider Type="Orleans.Storage.MemoryStorage" Name="MemoryStore" />
     <Provider Type="Orleans.Storage.Couchbase.CouchbaseStorage" Name="CouchbaseStore" UseGuidAsStorageKey="True" ConfigSectionName="couchbaseClients/couchbaseDataStore" />
    </StorageProviders>
    <SeedNode Address="localhost" Port="11111" />
  </Globals>
</OrleansConfiguration>

```
###### 3. Code
```csharp
 [StorageProvider(ProviderName = "CouchbaseStore")]
 public class BankAccount : IGrain,IBankAccount
 {
 }
```
