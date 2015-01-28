using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.Providers;

namespace Orleans.Storage.Couchbase
{
    /// <summary>
    /// Defines the interface for the lower level of JSON storage providers, i.e.
    /// the part that writes JSON strings to the underlying storage. The higher level
    /// maps between grain state data and JSON.
    /// </summary>
    /// <remarks>
    /// Having this interface allows most of the serialization-level logic
    /// to be implemented in a base class of the storage providers.
    /// </remarks>
    public interface IJSONStateDataManager : IDisposable
    {
        /// <summary>
        /// Deletes the grain state associated with a given key from the bucket
        /// </summary>
        /// <param name="key">The primary key of the object to delete</param>
        System.Threading.Tasks.Task Delete(string key);

        /// <summary>
        /// Reads grain state from storage.
        /// </summary>
        /// <param name="key">The primary key of the object to read.</param>
        /// <returns>A string containing a JSON representation of the entity, if it exists; null otherwise.</returns>
        System.Threading.Tasks.Task<string> Read(string key);

        /// <summary>
        /// Writes grain state to storage.
        /// </summary>
        /// <param name="bucket">The name of a bucket, such as a type name.</param>
        /// <param name="key">The primary key of the object to write.</param>
        /// <param name="entityData">A string containing a JSON representation of the entity.</param>
        System.Threading.Tasks.Task Write(string key, string entityData);
    }
}
