// ************************************************************************************************
// Microsoft Team Foundation
//
// Microsoft Confidential
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// Contents:    Utility methods for artifact linking.
// ************************************************************************************************
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Configuration;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Common
{
    public static class LinkingUtilities
    {
        public static bool IsToolTypeWellFormed(string tool)
        {
            if (String.IsNullOrEmpty(tool))
            {
                return false;
            }
            return tool.IndexOfAny(s_delimiters) < 0;
        }

        public static bool IsUriWellFormed(string artifactUri)
        {
            string serverName;
            string tool;
            string artifactType;
            string artifactMoniker;
            return IsUriWellFormed(artifactUri, out serverName, out tool,
                                    out artifactType, out artifactMoniker);
        }

        private static bool IsUriWellFormed(string artifactUri, 
            out string serverName,
            out string tool, 
            out string artifactType, 
            out string artifactMoniker)
        {           
            serverName = String.Empty;
            tool = null;
            artifactType = null;
            artifactMoniker = null;
            if (artifactUri == null)
            {
                return false;
            }

            //
            // uri format
            //
            // vstfs:///tooltype/artifacttype/artifactId
            //
            string tArtifactUri = artifactUri.Trim();

            //
            // verify the uri starts with vstfs:///
            //
            if (!tArtifactUri.StartsWith(VSTFS, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            tArtifactUri = tArtifactUri.Substring(VSTFS.Length, tArtifactUri.Length - VSTFS.Length);

            Char[] delimiters = URISEPARATOR.ToCharArray();
            string[] tokens = tArtifactUri.Split(delimiters, 3);

            if (tokens.Length != 3)
            {
                return false;
            }

            tool = tokens[0].Trim();
            if (!IsToolTypeWellFormed(tool))
            {
                return false;
            }

            artifactType = tokens[1].Trim();
            if (!IsArtifactTypeWellFormed(artifactType))
            {
                return false;
            }

            artifactMoniker = tokens[2].Trim();
            if (!IsArtifactToolSpecificIdWellFormed(artifactMoniker))
            {
                return false;
            }

            
            // DNS does not allow "/" in server name; however the URI format is extended.
#if false
            if(serverName.IndexOf('/') != -1)
            {
                return false;
            }
#endif
            return true;
        }

        public static bool IsArtifactTypeWellFormed(string artifactType)
        {
            if (String.IsNullOrEmpty(artifactType))
            {
                return false;
            }
            return artifactType.IndexOf('/') < 0;
        }
        public static bool IsArtifactToolSpecificIdWellFormed(string toolSpecificId)
        {
            if (String.IsNullOrEmpty(toolSpecificId))
            {
                return false;
            }
            return true;
        }

        public static bool IsArtifactIdWellFormed(ArtifactId artifactId)
        {
            if(artifactId == null)
            {
                return false;
            }

            return IsToolTypeWellFormed(artifactId.Tool) &&
                   IsArtifactTypeWellFormed(artifactId.ArtifactType) &&
                   IsArtifactToolSpecificIdWellFormed(artifactId.ToolSpecificId);
        }

        public static string EncodeUri(ArtifactId artifactId)
        {
            string uri = null;
            if(!LinkingUtilities.IsArtifactIdWellFormed(artifactId))
            {
                if (artifactId == null)
                {
                    throw new ArgumentNullException("artifactId");
                }
                else
                {
                    throw new ArgumentException(CommonResources.MalformedArtifactId(artifactId.ToString()), "artifactId");
                }
            }

            string tool = artifactId.Tool;
            string artifactType = artifactId.ArtifactType;
            string artifactMoniker = artifactId.ToolSpecificId;

#if !NETSTANDARD
            // TODO: [0] Since bisServerName can contain "/", you need
            // to handle problem cases in Url encoding.
            uri = VSTFS +  
                  UriUtility.UrlEncode(tool) + URISEPARATOR +
                  UriUtility.UrlEncode(artifactType) + URISEPARATOR +
                  UriUtility.UrlEncode(artifactMoniker);
#else
            uri = VSTFS + Uri.EscapeUriString(tool) + URISEPARATOR + Uri.EscapeUriString(artifactType) + URISEPARATOR + Uri.EscapeUriString(artifactMoniker);
#endif
            return uri;
        }

        public static ArtifactId DecodeUri(string uri)
        {
            string serverName = null;
            string tool = null;
            string artifactType = null;
            string toolSpecificId = null;

            ArtifactId artifactId = null;

            if(LinkingUtilities.IsUriWellFormed(
                uri, out serverName, out tool, out artifactType, out toolSpecificId))
            {
                artifactId = new ArtifactId();
                //TODO:[0] Since serverName can contain "/" you cannot use direct
                // UrlDecode. Handle problem cases.
                artifactId.VisualStudioServerNamespace = serverName;
                artifactId.Tool = UriUtility.UrlDecode(tool);
                artifactId.ArtifactType = UriUtility.UrlDecode(artifactType);
                artifactId.ToolSpecificId = UriUtility.UrlDecode(toolSpecificId);
            }  
            else
            {
                ArgumentUtility.CheckForNull(uri, "uri");
                throw new ArgumentException(CommonResources.MalformedUri(uri), "uri");
            }
            return artifactId;
        }

        // Remove duplicate links: If multiple tools return the same artifacts. The artifacts
        // will already contain links.
        public static ArrayList RemoveDuplicateArtifacts(ArrayList artifactList)
        {
            Hashtable ht = new Hashtable();
            foreach(Artifact artifact in artifactList)
            {
                ht[artifact.Uri] = artifact;
            }

            ArrayList noDuplicateList = new ArrayList();
            foreach(Artifact artifact in ht.Values)
            {
                noDuplicateList.Add(artifact);
            }
            
            return noDuplicateList;
        }

        /// <summary>
        /// Returns uri for a given url
        /// url is expected to be in the following format 
        /// http(s)://&lt;tooldisplayserver&gt;/&lt;toolname&gt;/&lt;artifacttype&gt;.aspx/artifactMoniker=&lt;artifactId&gt;
        /// </summary>
        /// <param name="artifactUrl"></param>
        /// <returns>artifactUri</returns>
        public static string GetArtifactUri(string artifactUrl)
        {
            if (String.IsNullOrEmpty(artifactUrl))
            {
                throw new ArgumentNullException(CommonResources.NullArtifactUrl());
            }

            string artifactUrlTrimmed = artifactUrl.Trim();

            if (!artifactUrlTrimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !artifactUrlTrimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(CommonResources.MalformedUrl(artifactUrl));
            }

            int monikerIndex = 0;

            //
            // search of .aspx/ArtifactMoniker to retreive the artifactId
            //
            monikerIndex = artifactUrlTrimmed.IndexOf(TOOLARTIFACTMONIKER, StringComparison.OrdinalIgnoreCase);
            if (monikerIndex == -1)
            {
                throw new ArgumentException(CommonResources.MalformedUrl(artifactUrl));
            }

            string artifactId = artifactUrlTrimmed.Substring(monikerIndex + TOOLARTIFACTMONIKER.Length);
            if (String.IsNullOrEmpty(artifactId))
            {
                throw new ArgumentException(CommonResources.MalformedUrl(artifactUrl));
            }

            //
            // retrieve the artifact type
            // url format http://servername/toolname/artifactType.aspx/artifactMoniker=artifactId
            //
            int artifactTypeIndex = artifactUrlTrimmed.LastIndexOf(URISEPARATOR, monikerIndex, StringComparison.Ordinal);

            //
            // make sure we have a valid artifactType
            //
            if ((artifactTypeIndex <= 0) || ((monikerIndex - artifactTypeIndex - 2) <= 0))
            {
                throw new ArgumentException(CommonResources.MalformedUrl(artifactUrl));
            }

            string artifactType = artifactUrlTrimmed.Substring(artifactTypeIndex + 1, monikerIndex - artifactTypeIndex - 1);

            int toolNameIndex = artifactUrlTrimmed.LastIndexOf(URISEPARATOR, (artifactTypeIndex - 1), StringComparison.Ordinal);

            //
            // make sure we have a valid toolName
            //
            if ((toolNameIndex <= 0) || ((artifactTypeIndex - toolNameIndex - 2) <= 0))
            {
                throw new ArgumentException(CommonResources.MalformedUrl(artifactUrl));
            }

            string toolName = artifactUrlTrimmed.Substring(toolNameIndex + 1, artifactTypeIndex - toolNameIndex - 1);

            return VSTFS + toolName + URISEPARATOR + artifactType + URISEPARATOR + artifactId;
        }


        /// <summary>
        /// Get Url for artifact for addressability in links.
        /// This will construct the artifact Url using 
        /// the server Url supplied.
        /// </summary>
        /// <param name="artifactDisplayUrl">artifactDisplayUrl from regristry DB extended attribute</param>
        /// <param name="artId">ArtifactId.</param>
        /// <param name="serverUrl">server Url.</param>
        /// <returns>ArtifactUrlExternal</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetArtifactUrl(string artifactDisplayUrl, ArtifactId artId, string serverUrl)
        {
            if (String.IsNullOrEmpty(artifactDisplayUrl))
            {
                // If no artifactDisplayUrl is specified, assume it is the same as the Tool for the artifact
                // Go ahead and add on the leading and trailing separators
                artifactDisplayUrl = String.Concat(URISEPARATOR, artId.Tool, URISEPARATOR);
            }
            else
            {
                artifactDisplayUrl = artifactDisplayUrl.Trim();
            }

            if (!String.IsNullOrEmpty(serverUrl))
            {
                //
                // create absolute path if relative path is given or no artifactdisplay url is given
                //
                if (artifactDisplayUrl.StartsWith(URISEPARATOR, StringComparison.OrdinalIgnoreCase))
                {
                    if (serverUrl.EndsWith(URISEPARATOR, StringComparison.OrdinalIgnoreCase))
                    {
                        // skip the uri separator in the artifact url
                        artifactDisplayUrl = String.Concat(serverUrl, artifactDisplayUrl.Substring(1));
                    }
                    else
                    {
                        // simply concat the strings together
                        artifactDisplayUrl = String.Concat(serverUrl, artifactDisplayUrl);
                    }
                }
            }

            if (!artifactDisplayUrl.EndsWith(URISEPARATOR, StringComparison.OrdinalIgnoreCase))
            {
                artifactDisplayUrl = String.Concat(artifactDisplayUrl, URISEPARATOR);
            }

            return (String.Concat(artifactDisplayUrl, artId.ArtifactType, TOOLARTIFACTMONIKER, artId.ToolSpecificId));
        }

        /// <summary>
        /// The server Url for external access
        /// </summary>
        /// <param name="serverUri">server Uri</param>
        /// <returns>Server Url</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetServerUrl(Uri serverUri)
        {
            string retVal;

            // Set default server Url from TF server object.
            // Look for a config parameter for the server Url
            // (includes protocol (https), server name and port).
            string configUrl = (string)ConfigurationManager.AppSettings["TFSUrlPublic"];
            if (String.IsNullOrEmpty(configUrl))
            {
                retVal = serverUri.ToString();
            }
            else
            {
                retVal = configUrl;
            }

            return retVal;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string VSTFS = "vstfs:///";
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string URISEPARATOR = "/";
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string TOOLARTIFACTMONIKER = ".aspx?artifactMoniker=";
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string SERVICE = "/Service/";

        private static readonly char[] s_delimiters = new[] { '/', '\\', '.' };
    }
}
