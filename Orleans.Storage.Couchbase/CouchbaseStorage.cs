using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core;
using Couchbase.Configuration.Client;
using Newtonsoft.Json;

namespace Orleans.Storage.Couchbase
{
    /// <summary>
    /// A Couchbase storage provider.
    /// </summary>
    /// <remarks>
    /// The storage provider should be included in a deployment by adding this line to the Orleans server configuration file:
    /// 
    ///     <Provider Type="Orleans.Storage.Couchbase.CouchbaseStorage" Name="CouchbaseStore" Database="db-name" ConnectionString="mongodb://YOURHOSTNAME:27017/" />
    ///
    /// and this line to any grain that uses it:
    /// 
    ///     [StorageProvider(ProviderName = "CouchbaseStore")]
    /// 
    /// The name 'CouchbaseStore' is an arbitrary choice.
    /// </remarks>
    public class CouchbaseStorage : BaseJSONStorageProvider
    {
        public string BucketPassword { get; set; }
        public string BucketName { get; set; }
        public Uri[] Servers { get; set; }
        public bool UseSsl { get; set; }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;
            this.BucketName = config.Properties["BucketName"];
            this.BucketPassword = config.Properties["BucketPassword"];
            var useSslString = config.Properties["UseSsl"];
            var servers = config.Properties["Servers"];

            bool useSsl = false;
            if (!string.IsNullOrEmpty(useSslString))
                bool.TryParse(useSslString, out useSsl);
            this.UseSsl = useSsl;

            this.Servers = ParseServerUris(servers);

            if (string.IsNullOrWhiteSpace(BucketName)) throw new ArgumentException("BucketName property not set");
            if (string.IsNullOrWhiteSpace(BucketPassword)) throw new ArgumentException("BucketPassword property not set");
            if (this.Servers.Length == 0) throw new ArgumentException("Servers property not set");

            DataManager = new GrainStateCouchbaseDataManager(BucketName, BucketPassword, Servers, UseSsl);
            return base.Init(name, providerRuntime, config);
        }

        private Uri[] ParseServerUris(string uriConfigString)
        {
            List<Uri> servers = new List<Uri>();

            if (!string.IsNullOrEmpty(uriConfigString))
            {
                string[] uris = uriConfigString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (uris != null && uris.Length > 0)
                {
                    foreach (string uri in uris)
                    {
                        servers.Add(new Uri(uri));
                    }
                }
            }

            return servers.ToArray();
        }
    }

    /// <summary>
    /// Interfaces with a Couchbase database driver.
    /// </summary>
    internal class GrainStateCouchbaseDataManager : IJSONStateDataManager
    {
        public GrainStateCouchbaseDataManager(string bucketName, string bucketPassword, Uri[] servers, bool useSsl = false)
        {
            var config = new ClientConfiguration()
            {
                Servers = servers.ToList(),
                UseSsl = useSsl
            };

            ClusterHelper.Initialize(config);
            this._bucket = ClusterHelper.Get().OpenBucket(bucketName, bucketPassword);
        }

        /// <summary>
        /// Deletes a file representing a grain state object.
        /// </summary>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task Delete(string key)
        {
            var result = this._bucket.Remove(key);

            if (!result.Success)
            {
                var exist = await this._bucket.GetAsync<string>(key);

                if (exist.Success)
                    this._bucket.Remove(key);
            }
        }

        /// <summary>
        /// Reads a file representing a grain state object.
        /// </summary>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<string> Read(string key)
        {
            var entity = await this._bucket.GetAsync<string>(key);
            var result = string.Empty;

            if (entity.Success)
                result = entity.Value;

            return result;
        }

        /// <summary>
        /// Writes a file representing a grain state object.
        /// </summary>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task Write(string key, string entityData)
        {
            this._bucket.Upsert(key, entityData);

            return TaskDone.Done;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            this._bucket.Dispose();
            ClusterHelper.Close();
        }

        private IBucket _bucket;
    }
}
