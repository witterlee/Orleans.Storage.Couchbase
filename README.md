# Orleans.Storage.Couchbase
   Orleans Storage Provider Of Couchbase
##use case

```csharp
  <Provider Type="Orleans.Storage.Couchbase.CouchbaseStorage" Name="CouchbaseStore" BucketName="bucketName" BucketPassword="bucketPassword" UseSsl="true" Servers="http://192.168.0.100:8091/pools;http://192.168.0.101:8091/pools"  />
```
   
```csharp
 [StorageProvider(ProviderName = "CouchbaseStore")]
 public class BankAcount : IGrain
 {
 }
```
 
 ##reminder
 the newest Couchbase-net-client has some bug use with Orleans.  
 I submit a pull request to fixed this problem.(https://github.com/couchbase/couchbase-net-client/pull/34)  
 so this libary use a couchbase-net-client compile from https://github.com/weitaolee/couchbase-net-client  
 when Couchbase-net-client release a new version and slove this problem,I will use a nuget package to replace my branch.
 so now `you should use couchbase-net-client` in libs