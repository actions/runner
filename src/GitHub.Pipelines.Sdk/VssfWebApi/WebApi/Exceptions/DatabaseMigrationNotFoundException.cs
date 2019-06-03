using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Zeus
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DatabaseMigrationNotFoundException", "GitHub.Services.Zeus.DatabaseMigrationNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DatabaseMigrationNotFoundException : VssServiceException
    {
        public DatabaseMigrationNotFoundException(int migrationId)
            : base(ZeusWebApiResources.DatabaseMigrationNotFoundException(migrationId))
        {
        }

        public DatabaseMigrationNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DatabaseMigrationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DuplicateDatabaseMigrationException", "GitHub.Services.Zeus.DuplicateDatabaseMigrationException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DuplicateDatabaseMigrationException : VssServiceException
    {
        public DuplicateDatabaseMigrationException (int migrationId)
            : base(ZeusWebApiResources.DatabaseMigrationNotFoundException(migrationId))
        {
        }

        public DuplicateDatabaseMigrationException (String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DuplicateDatabaseMigrationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
