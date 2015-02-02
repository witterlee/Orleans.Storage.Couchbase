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
using Orleans.Runtime;

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
    public class CouchbaseStorage : IStorageProvider
    {
        /// <summary>
        /// Logger object
        /// </summary>
        public Logger Log { get; protected set; }
        public string ConfigSectionName { get; set; }
        /// <summary>
        /// Storage provider name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Data manager instance
        /// </summary>
        /// <remarks>The data manager is responsible for reading and writing JSON strings.</remarks>
        GrainStateCouchbaseDataManager DataManager { get; set; }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns> 
        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;
            this.ConfigSectionName = config.Properties["ConfigSectionName"];

            if (string.IsNullOrWhiteSpace(ConfigSectionName)) throw new ArgumentException("ConfigSectionName property not set");
            var configSection = ReadConfig(ConfigSectionName);
            DataManager = new GrainStateCouchbaseDataManager(configSection);
            Log = providerRuntime.GetLogger(this.GetType().FullName);
            return TaskDone.Done;
        }

        /// <summary>
        /// Closes the storage provider during silo shutdown.
        /// </summary>
        /// <returns>Completion promise for this operation.</returns>
        public Task Close()
        {
            if (DataManager != null)
                DataManager.Dispose();
            DataManager = null;
            return TaskDone.Done;
        }

        /// <summary>
        /// Reads persisted state from the backing store and deserializes it into the the target
        /// grain state object.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">A reference to an object to hold the persisted state of the grain.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");
            var entityData = await DataManager.Read(grainReference.ToKeyString());
            if (!string.IsNullOrEmpty(entityData))
            {
                ConvertFromStorageFormat(grainState, entityData);
            }
        }

        /// <summary>
        /// Writes the persisted state from a grain state object into its backing store.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">A reference to an object holding the persisted state of the grain.</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");
            var entityData = ConvertToStorageFormat(grainType, grainState);
            return DataManager.Write(grainReference.ToKeyString(), entityData);
        }

        /// <summary>
        /// Removes grain state from its backing store, if found.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">An object holding the persisted state of the grain.</param>
        /// <returns></returns>
        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");
            DataManager.Delete(grainReference.ToKeyString());
            return TaskDone.Done;
        }

        /// <summary>
        /// Serializes from a grain instance to a JSON document.
        /// </summary>
        /// <param name="grainState">Grain state to be converted into JSON storage format.</param>
        /// <remarks>  
        /// </remarks>
        protected static string ConvertToStorageFormat(string grainType, IGrainState grainState)
        {
            IDictionary<string, object> dataValues = grainState.AsDictionary();
            //store _Type into couchbase
            dataValues["_Type"] = grainType;
            return JsonConvert.SerializeObject(dataValues);
        }

        /// <summary>
        /// Constructs a grain state instance by deserializing a JSON document.
        /// </summary>
        /// <param name="grainState">Grain state to be populated for storage.</param>
        /// <param name="entityData">JSON storage format representaiton of the grain state.</param>
        protected static void ConvertFromStorageFormat(IGrainState grainState, string entityData)
        {
            var setting = new JsonSerializerSettings();
            object data = JsonConvert.DeserializeObject(entityData, grainState.GetType());
            var dict = ((IGrainState)data).AsDictionary();
            grainState.SetAll(dict);
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
    internal class GrainStateCouchbaseDataManager
    {
        public GrainStateCouchbaseDataManager(CouchbaseClientSection configSection)
        {
            var config = new ClientConfiguration(configSection);
            _cluster = new Cluster(config);

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

        private static Cluster _cluster;
        private IBucket _bucket;
    }
}
