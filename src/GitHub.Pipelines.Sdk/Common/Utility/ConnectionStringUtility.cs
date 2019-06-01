using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ConnectionStringUtility
    {
        /// <summary>
        /// Mask the password portion of the valid sql connection string.  Use this for tracing.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static String MaskPassword(string connectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = SecretUtility.PasswordMask;
                return builder.ToString();
            }

            return connectionString;
        }

        /// <summary>
        /// Replaces Initial catalog (database name) in the connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to update.</param>
        /// <param name="databaseName">New value for the Initial Catalog (database name). 
        /// If this parameter is null, Initial Catalog is removed from the connection string.</param>
        /// <returns></returns>
        public static string ReplaceInitialCatalog(string connectionString, string databaseName)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            // Note: SqlConnectionStringBuilder throws ArgumentNullException when I try to set InitialCatalog to null.
            if (string.IsNullOrEmpty(databaseName))
            {
                builder.Remove(c_initialCatalogKeyword);
            }
            else
            {
                builder.InitialCatalog = databaseName;
            }

            return builder.ToString();
        }

#if !NETSTANDARD
        /// <summary>
        /// Given a string, that *should* be either a plain text connection string or an encrypted connection string
        /// try to determine if it is encrypted, and if so, decrypt it, then parse via SqlConnectionStringBuilder and
        /// return a normalized version. Will return null (and not throw) if anything goes wrong.
        /// Caller may choose to throw or not...
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string DecryptAndNormalizeConnectionString(string inputString)
        {
            try
            {
                string connectionString = inputString;
                if (inputString.IndexOf(c_dataSourceToken, StringComparison.OrdinalIgnoreCase) == -1)
                {
#pragma warning disable 0618
                    //can't find dataSource token in the string, it is probably encrypted,
                    //this is now backcompat, only password is encrypted in new deployments...
                    connectionString = EncryptionUtility.TryDecryptSecretInsecure(inputString);
#pragma warning restore 0618
                }

                SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

                return connectionStringBuilder.ConnectionString;
            }
            catch (Exception)
            {
                return null;
            }
        }
#endif
        
        private const string c_initialCatalogKeyword = "Initial Catalog";
        private const string c_dataSourceToken = "Data Source=";
        
    }
}
