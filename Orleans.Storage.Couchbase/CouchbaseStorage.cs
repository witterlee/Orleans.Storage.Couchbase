using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core;
using Couchbase.IO;
using Couchbase.Configuration.Client;
using Newtonsoft.Json;
using System.Configuration;
using Couchbase.Configuration.Client.Providers;

namespace Orleans.Storage.Couchbase
{
    /// <summary>
    /// A Couchbase storage provider.
    /// </summary>
    /// <remarks>
    /// The storage provider should be included in a deployment by adding this line to the Orleans server configuration file:
    /// 
    ///     <Provider Type="Orleans.Storage.Couchbase.CouchbaseStorage" Name="CouchbaseStore" ConfigSectionName="couchbaseClients/couchbaseDataStore" /> 
    /// and this line to any grain that uses it:
    /// 
    ///     [StorageProvider(ProviderName = "CouchbaseStore")]
    /// 
    /// The name 'CouchbaseStore' is an arbitrary choice.
    /// </remarks>
    public class CouchbaseStorage : BaseJSONStorageProvider
    {
        public string ConfigSectionName { get; set; }
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
            this.ConfigSectionName = config.Properties["ConfigSectionName"];

            if (string.IsNullOrWhiteSpace(ConfigSectionName)) throw new ArgumentException("ConfigSectionName property not set");
            var configSection = ReadConfig(ConfigSectionName);
            DataManager = new GrainStateCouchbaseDataManager(configSection);
            return base.Init(name, providerRuntime, config);
        }
 
        private CouchbaseClientSection ReadConfig(string sectionName)
        {
            var section = (CouchbaseClientSection)ConfigurationManager.GetSection(sectionName);
            if (section.Servers.Count == 0) throw new ArgumentException("Couchbase servers not set");

            return section;
        }
    }

    /// <summary>
    /// Interfaces with a Couchbase database driver.
    /// </summary>
    internal class GrainStateCouchbaseDataManager : IJSONStateDataManager
    {
        public GrainStateCouchbaseDataManager(CouchbaseClientSection configSection)
        {
            var config = new ClientConfiguration(configSection);
            ClusterHelper.Initialize(config);

            if (configSection.Buckets.Count > 0)
            {
                var buckets = new BucketElement[configSection.Buckets.Count];
                configSection.Buckets.CopyTo(buckets, 0);

                var bucketSetting = buckets.First();
                this._bucket = ClusterHelper.Get().OpenBucket(bucketSetting.Name, bucketSetting.Password);

            }
            else
                this._bucket = ClusterHelper.Get().OpenBucket();
        }

        /// <summary>
        /// Deletes a file representing a grain state object.
        /// </summary>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task Delete(string key)
        {
            var result = this._bucket.Remove(key);

            if (!result.Success)
            {
                var exist = this._bucket.Get<string>(key);

                if (exist.Success)
                    this._bucket.Remove(key);
            }

            return TaskDone.Done;
        }

        /// <summary>
        /// Reads a file representing a grain state object.
        /// </summary>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task<string> Read(string key)
        {
            var result = this._bucket.Get<string>(key);
            var data = string.Empty;
            if (result.Status == ResponseStatus.Success)
                data = result.Value;
            else if (result.Status == ResponseStatus.KeyNotFound)
                data = string.Empty;
            else if (result.Status == ResponseStatus.Busy)
                throw new Exception("couchbase server too busy");
            else if (result.Status == ResponseStatus.OperationTimeout)
                throw new Exception("read from couchbase time out");

            return Task.FromResult(data);
        }

        /// <summary>
        /// Writes a file representing a grain state object.
        /// </summary>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task Write(string key, string entityData)
        {
            var result = this._bucket.Upsert(key, entityData);

            if (result.Status == ResponseStatus.Success)
                return TaskDone.Done;
            else if (result.Status == ResponseStatus.Busy)
                throw new Exception("couchbase server too busy");
            else if (result.Status == ResponseStatus.OperationTimeout)
                throw new Exception("write to couchbase time out");
            else if (result.Status == ResponseStatus.ValueTooLarge)
                throw new Exception("data value too large to write");

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
