# Orleans.Storage.Couchbase
   Orleans Storage Provider Of Couchbase

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
      <Provider Type="Orleans.Storage.Couchbase.CouchbaseStorage" Name="CouchbaseStore" ConfigSectionName="couchbaseClients/couchbaseDataStore" />
    </StorageProviders>
    <SeedNode Address="localhost" Port="11111" />
  </Globals>
</OrleansConfiguration>

```
###### 3. Code
```csharp
 [StorageProvider(ProviderName = "CouchbaseStore")]
 public class BankAccount : IGrain,IBacnkAccount
 {
 }
```
 
## REMINDER  
 the newest Couchbase-net-client has some bug use with Orleans.  
 I submit a pull request to fixed this problem.(https://github.com/couchbase/couchbase-net-client/pull/34)  
 so this libary use a couchbase-net-client compile from https://github.com/weitaolee/couchbase-net-client  
 when Couchbase-net-client release a new version and slove this problem,I will use a nuget package to replace my branch.
 so now `you should use couchbase-net-client` in libs