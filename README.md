## Orleans.Storage.Couchbase
   Orleans Storage Provider Of Couchbase
#use case

```csharp
  <Provider Type="Orleans.Storage.Couchbase.CouchbaseStorage" Name="CouchbaseStore" BucketName="bucketName" BucketPassword="bucketPassword" UseSsl="true" Servers="http://192.168.0.100:8091/pools;http://192.168.0.101:8091/pools"  />
```
   
```csharp
 [StorageProvider(ProviderName = "CouchbaseStore")]
 public class BankAcount : IGrain
 {
 }