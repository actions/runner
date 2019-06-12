using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitHub.Services.Common.ClientStorage
{
    /// <summary>
    /// An interface for accessing client data stored locally.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)] // for internal use
    public interface IVssClientStorage : IVssClientStorageReader, IVssClientStorageWriter
    {
        /// <summary>
        /// Much like the System.IO.Path.Combine method, this method puts together path segments into a path using
        /// the appropriate path delimiter.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        string PathKeyCombine(params string[] paths);

        /// <summary>
        /// The path segment delimiter used by this storage mechanism.
        /// </summary>
        char PathSeparator { get; }
    }

    /// <summary>
    /// An interface for reading from local data storage
    /// </summary>
    public interface IVssClientStorageReader
    {
        /// <summary>
        /// Reads one entry from the storage.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="path">This is the path key for the data to retrieve.</param>
        /// <returns>Returns the value stored at the given path as type T</returns>
        T ReadEntry<T>(string path);

        /// <summary>
        /// Reads one entry from the storage.  If the entry does not exist or can not be converted to type T, the default value provided will be returned.
        /// When T is not a simple type, and there is extra logic to determine the default value, the pattern: ReadEntry&lt;T&gt;(path) && GetDefault(); is
        /// preferred, so that method to retrieve the default is not evaluated unless the entry does not exist.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="path">This is the path key for the data to retrieve.</param>
        /// <param name="defaultValue">The value to return if the key does not exist or the value can not be converted to type T</param>
        /// <returns></returns>
        T ReadEntry<T>(string path, T defaultValue);

        /// <summary>
        /// Returns all entries under the path provided whose values can be converted to T.  If path = "root\mydata", then this will return all entries where path begins with "root\mydata\".
        /// </summary>
        /// <typeparam name="T">The type for the entries to return.</typeparam>
        /// <param name="path">The path pointing to the branch of entries to return.</param>
        /// <returns></returns>
        IDictionary<string, T> ReadEntries<T>(string path);
    }

    /// <summary>
    /// An interface for writing to local data storage
    /// </summary>
    public interface IVssClientStorageWriter
    {
        /// <summary>
        /// Write one entry into the local data storage.
        /// </summary>
        /// <param name="path">This is the key for the data to store.  Providing a path allows data to be accessed hierarchicaly.</param>
        /// <param name="value">The value to store at the specified path. Setting his to NULL will remove the entry.</param>
        void WriteEntry(string path, object value);

        /// <summary>
        /// Writes a set of entries to the writer, which provides efficiency benefits over writing each entry individually.
        /// It also ensures that the either all of the entries are written or in the case of an error, no entries are written.
        /// Setting a value to NULL, will remove the entry.
        /// </summary>
        /// <param name="entries"></param>
        void WriteEntries(IEnumerable<KeyValuePair<string, Object>> entries);
    }
}
