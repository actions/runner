// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.WebApi.Location
{
    /// <summary>
    /// 
    /// </summary>
    internal class ServerMapData
    {
        /// <summary>
        /// 
        /// </summary>
        public ServerMapData()
            : this(Guid.Empty, Guid.Empty)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="serviceOwner"></param>
        public ServerMapData(Guid serverId, Guid serviceOwner)
        {
            ServerId = serverId;
            ServiceOwner = serviceOwner;
        }

        /// <summary>
        /// 
        /// </summary>
        public Guid ServerId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Guid ServiceOwner
        {
            get;
            set;
        }
    }

    internal static class LocationServerMapCache
    {
        /// <summary>
        /// Finds the location for the specified guid.  If it is not found, null
        /// is returned.
        /// </summary>
        /// <param name="serverId">The server instance id associated with the
        /// desired location service url.</param>
        /// <returns>The location of the location service for this server or null
        /// if the guid is not found.</returns>
        public static String ReadServerLocation(Guid serverId, Guid serviceOwner)
        {
            try
            {
                EnsureCacheLoaded();
                s_accessLock.EnterReadLock();

                // Iterate through the dictionary to find the location we are looking for
                foreach (KeyValuePair<String, ServerMapData> pair in s_serverMappings)
                {
                    if (Guid.Equals(serverId, pair.Value.ServerId) &&
                        Guid.Equals(serviceOwner, pair.Value.ServiceOwner))
                    {
                        return pair.Key;
                    }
                }

                return null;
            }
            finally
            {
                if (s_accessLock.IsReadLockHeld)
                {
                    s_accessLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location"></param>
        /// <returns>The guid for this location or Guid.Empty if the location
        /// does not have an entry.</returns>
        public static ServerMapData ReadServerData(String location)
        {
            try
            {
                EnsureCacheLoaded();
                s_accessLock.EnterReadLock();

                ServerMapData serverData;
                if (!s_serverMappings.TryGetValue(location, out serverData))
                {
                    return new ServerMapData();
                }

                return serverData;
            }
            finally
            {
                if (s_accessLock.IsReadLockHeld)
                {
                    s_accessLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// If this call is not a change, nothing will be done.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="serverId"></param>
        /// <param name="serviceOwner"></param>
        /// <returns>True if this is the first time the mapping was written.</returns>
        public static Boolean EnsureServerMappingExists(String location, Guid serverId, Guid serviceOwner)
        {
            try
            {
                EnsureCacheLoaded();
                s_accessLock.EnterWriteLock();

                // See if this is an update or an add to optimize writing the disk.
                Boolean isNew = true;
                ServerMapData storedData;
                if (s_serverMappings.TryGetValue(location, out storedData))
                {
                    if (storedData.ServerId.Equals(serverId) &&
                        storedData.ServiceOwner.Equals(serviceOwner))
                    {
                        return false;
                    }
                    isNew = false;
                }

                // Make the change in the cache
                s_serverMappings[location] = new ServerMapData(serverId, serviceOwner);

                s_accessLock.ExitWriteLock();

                // Persist the change
                return TryWriteMappingToDisk(location, serverId, serviceOwner, isNew);
            }
            finally
            {
                if (s_accessLock.IsWriteLockHeld)
                {
                    s_accessLock.ExitWriteLock();
                }
            }
        }

        private static void EnsureCacheLoaded()
        {
            if (s_cacheFreshLocally || s_cacheUnavailable)
            {
                return;
            }

            FileStream file = null;

            try
            {
                s_accessLock.EnterWriteLock();

                if (s_cacheFreshLocally)
                {
                    return;
                }

                // actually load the cache from disk
                // Open the file, allowing concurrent reads.
                // Do not create the file if it does not exist.
                XmlDocument document = XmlUtility.OpenXmlFile(out file, FilePath, FileShare.Read, false);

                if (document != null)
                {
                    // This is an existing document, get the root node
                    XmlNode documentNode = document.ChildNodes[0];

                    // Load all of the mappings
                    foreach (XmlNode mappingNode in documentNode.ChildNodes)
                    {
                        String location = mappingNode.Attributes[s_locationAttribute].InnerText;
                        Guid guid = XmlConvert.ToGuid(mappingNode.Attributes[s_guidAttribute].InnerText);

                        // Legacy server case: Don't error out if the existing file doesn't have the owner attribute.
                        // Once the server is updated the next connect call should update this record
                        Guid serviceOwner = Guid.Empty;
                        if (mappingNode.Attributes[s_ownerAttribute] != null)
                        {
                            serviceOwner = XmlConvert.ToGuid(mappingNode.Attributes[s_ownerAttribute].InnerText);
                        }

                        // If the service owner is absent, then the server is on-prem
                        if (Guid.Empty == serviceOwner)
                        {
                            serviceOwner = ServiceInstanceTypes.TFSOnPremises;
                        }

                        s_serverMappings[location] = new ServerMapData(guid, serviceOwner);
                    }
                }

                // Hook up the file system watcher so we know if we need to invalidate our cache
                if (s_fileWatcher == null)
                {
                    String directoryToWatch = VssClientSettings.ClientCacheDirectory;

                    // Ensure the directory exists, otherwise FileSystemWatcher will throw.
                    if (!Directory.Exists(directoryToWatch))
                    {
                        Directory.CreateDirectory(directoryToWatch);
                    }

                    s_fileWatcher = new FileSystemWatcher(directoryToWatch, s_fileName);
                    s_fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    s_fileWatcher.Changed += new FileSystemEventHandler(s_fileWatcher_Changed);
                }
            }
            catch (Exception)
            {
                // It looks like something is wrong witht he cahce, lets just hide this
                // exception and work without it.
                s_cacheUnavailable = true;
            }
            finally
            {
                s_cacheFreshLocally = true;

                if (file != null)
                {
                    file.Close();
                }

                if (s_accessLock.IsWriteLockHeld)
                {
                    s_accessLock.ExitWriteLock();
                }
            }
        }

        static void s_fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            s_cacheFreshLocally = false;
        }

        /// <summary>
        /// Writes the mapping to disk if the cache is available.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="serverGuid"></param>
        /// <param name="isNew"></param>
        /// <returns>True if the write succeeded</returns>
        private static Boolean TryWriteMappingToDisk(String location, Guid serverGuid, Guid serviceOwner, Boolean isNew)
        {
            if (s_cacheUnavailable)
            {
                return false;
            }

            FileStream file = null;

            try
            {
                // Open the file with an exclusive lock                
                XmlDocument existingDocument = XmlUtility.OpenXmlFile(out file, FilePath, FileShare.None, true);

                // Only allow one writer at a time
                lock (s_cacheMutex)
                {
                    XmlNode documentNode = null;
                    if (existingDocument == null)
                    {
                        // This is a new document, create the xml
                        existingDocument = new XmlDocument();

                        // This is the first entry, create the document node and add the child
                        documentNode = existingDocument.CreateNode(XmlNodeType.Element, s_documentXmlText, null);
                        existingDocument.AppendChild(documentNode);

                        AddMappingNode(documentNode, location, serverGuid, serviceOwner);
                    }
                    else
                    {
                        // Get the root document node
                        documentNode = existingDocument.ChildNodes[0];

                        // If this is a new mapping, just add it to the document node
                        if (isNew)
                        {
                            AddMappingNode(documentNode, location, serverGuid, serviceOwner);
                        }
                        else
                        {
                            // This is some form of update.  Find the node with the location and update
                            // the guid.  
                            foreach (XmlNode mappingNode in documentNode.ChildNodes)
                            {
                                if (StringComparer.OrdinalIgnoreCase.Equals(mappingNode.Attributes[s_locationAttribute].InnerText, location))
                                {
                                    // This is the one we have to update, do so now
                                    mappingNode.Attributes[s_guidAttribute].InnerText = XmlConvert.ToString(serverGuid);

                                    // For compatibility with older OMs with the same major version, persist the on-prem service owner as empty.
                                    if (ServiceInstanceTypes.TFSOnPremises == serviceOwner)
                                    {
                                        serviceOwner = Guid.Empty;
                                    }

                                    // Legacy server case: Let's be resilient to the persisted document not already having an owner attribute
                                    XmlAttribute ownerAttribute = existingDocument.CreateAttribute(s_ownerAttribute);
                                    ownerAttribute.InnerText = XmlConvert.ToString(serviceOwner);
                                    mappingNode.Attributes.Append(ownerAttribute);
                                }
                            }
                        }
                    }

                    // Reset the file stream.
                    file.SetLength(0);
                    file.Position = 0;

                    // Save the file.
                    existingDocument.Save(file);

                    return true;
                }
            }
            catch (Exception)
            {
                // It looks like we are being denied access to the cache, lets just hide this
                // exception and work without it.
                s_cacheUnavailable = true;
                return false;
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }
        }

        private static void AddMappingNode(XmlNode parentNode, String location, Guid guid, Guid owner)
        {
            XmlNode mappingNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, s_mappingXmlText, null);
            parentNode.AppendChild(mappingNode);

            // Write the mapping as attributes
            XmlUtility.AddXmlAttribute(mappingNode, s_locationAttribute, location);
            XmlUtility.AddXmlAttribute(mappingNode, s_guidAttribute, XmlConvert.ToString(guid));

            // For compatibility with older OMs with the same major version, persist the on-prem service owner as empty.
            if (ServiceInstanceTypes.TFSOnPremises == owner)
            {
                owner = Guid.Empty;
            }

            // Legacy server case: If the server did not send back ServiceOwner in the connectionData
            // let's just do what we used to do to not break anything.
            // Eventually we can remove this if-guard
            if (owner != Guid.Empty)
            {
                XmlUtility.AddXmlAttribute(mappingNode, s_ownerAttribute, XmlConvert.ToString(owner));
            }
        }

        private static String FilePath
        {
            get
            {
                if (s_filePath == null)
                {
                    s_filePath = Path.Combine(VssClientSettings.ClientCacheDirectory, s_fileName);
                }

                return s_filePath;
            }
        }

        private static ReaderWriterLockSlim s_accessLock = new ReaderWriterLockSlim();

        private static Dictionary<String, ServerMapData> s_serverMappings = new Dictionary<String, ServerMapData>(StringComparer.OrdinalIgnoreCase);

        private static String s_filePath;

        private static FileSystemWatcher s_fileWatcher;

        /// <summary>
        /// This is used to keep track of whether or not our in-memory cache is fresh with regards
        /// to our persistent cache on disk.
        /// </summary>
        private static Boolean s_cacheFreshLocally = false;

        /// <summary>
        /// This is true if we do not have access to the cache file
        /// </summary>
        private static Boolean s_cacheUnavailable = false;

        private static readonly String s_fileName = "LocationServerMap.xml";
        private static readonly String s_documentXmlText = "LocationServerMappings";
        private static readonly String s_mappingXmlText = "ServerMapping";
        private static readonly String s_locationAttribute = "location";
        private static readonly String s_guidAttribute = "guid";
        private static readonly String s_ownerAttribute = "owner";

        private static Object s_cacheMutex = new Object();
    }
}
