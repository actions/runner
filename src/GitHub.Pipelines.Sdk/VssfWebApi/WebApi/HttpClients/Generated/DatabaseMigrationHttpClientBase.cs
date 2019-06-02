/*
* ---------------------------------------------------------
* Copyright(C) Microsoft Corporation. All rights reserved.
* ---------------------------------------------------------
* 
* ---------------------------------------------------------
* Generated file, DO NOT EDIT
* ---------------------------------------------------------
*
* See following wiki page for instructions on how to regenerate:
*   https://vsowiki.com/index.php?title=Rest_Client_Generation
*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Zeus
{
    [ResourceArea(DatabaseMigrationLocationIds.ResourceString)]
    public abstract class DatabaseMigrationHttpClientBase : VssHttpClientBase
    {
        public DatabaseMigrationHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public DatabaseMigrationHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public DatabaseMigrationHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public DatabaseMigrationHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public DatabaseMigrationHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationId"></param>
        public virtual Task<HttpResponseMessage> DeleteDatabaseMigrationAsync(
            int migrationId,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("d56223df-8ccd-45c9-89b4-eddf69240000");
            Object routeValues = new { migrationId = migrationId };

            return SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationId"></param>
        public virtual Task<DatabaseMigration> GetDatabaseMigrationAsync(
            int migrationId,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d56223df-8ccd-45c9-89b4-eddf69240000");
            Object routeValues = new { migrationId = migrationId };

            return SendAsync<DatabaseMigration>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        public virtual Task<List<DatabaseMigration>> GetDatabaseMigrationsAsync(
        
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d56223df-8ccd-45c9-89b4-eddf69240000");

            return SendAsync<List<DatabaseMigration>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="migration"></param>
        public virtual Task<DatabaseMigration> QueueDatabaseMigrationAsync(
            DatabaseMigration migration,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("d56223df-8ccd-45c9-89b4-eddf69240000");
            HttpContent content = new ObjectContent<DatabaseMigration>(migration, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DatabaseMigration>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="migration"></param>
        public virtual Task<DatabaseMigration> UpdateDatabaseMigrationAsync(
            DatabaseMigration migration,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("d56223df-8ccd-45c9-89b4-eddf69240000");
            HttpContent content = new ObjectContent<DatabaseMigration>(migration, new VssJsonMediaTypeFormatter(true));

            return SendAsync<DatabaseMigration>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content
            );
        }
    }
}
