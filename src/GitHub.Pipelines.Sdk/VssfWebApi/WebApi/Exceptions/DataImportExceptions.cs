using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.WebApi
{
    [Serializable]
    public abstract class DataImportException : VssServiceException
    {
        public DataImportException(string message)
            : base(message)
        {
            this.MarkAsFatalServicingOrchestrationException();
        }

        public DataImportException(String message, Exception innerException)
            : base(message, innerException)
        {
            this.MarkAsFatalServicingOrchestrationException();
        }

        protected DataImportException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.MarkAsFatalServicingOrchestrationException();
        }
    }

    /// <summary>
    /// An extended property in the dacpac or database is not allowed
    /// </summary>
    [Serializable]
    public class InvalidSourceExtendedPropertyValueException : DataImportException
    {
        public InvalidSourceExtendedPropertyValueException(string propertyName, string expectedValue, string propertyValue)
            : base(DataImportResources.ImportInvalidSourceExtendedPropertyValue(propertyName, expectedValue, propertyValue))
        {
        }

        public InvalidSourceExtendedPropertyValueException(string message)
            : base(message)
        {
        }

        public InvalidSourceExtendedPropertyValueException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidSourceExtendedPropertyValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// A required extended property was not found in the dacpac or database
    /// </summary>
    [Serializable]
    public class MissingSourceExtendedPropertyException : DataImportException
    {
        public MissingSourceExtendedPropertyException(string message)
            : base(message)
        {
        }

        public MissingSourceExtendedPropertyException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MissingSourceExtendedPropertyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The version of SQLPackage.exe used to create the dacpac is not supported
    /// </summary>
    [Serializable]
    public class SqlPackageVersionNotSupportedException : DataImportException
    {
        public SqlPackageVersionNotSupportedException(string message)
            : base(message)
        {
        }

        public SqlPackageVersionNotSupportedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SqlPackageVersionNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The size of customer dacpac exceeded threshold
    /// </summary>
    [Serializable]
    public class MaxDacpacSizeExceededException : DataImportException
    {
        public MaxDacpacSizeExceededException(string dacpacSizeInGB, string thresholdSizeInGB)
            : base(DataImportResources.MaxDacpacSizeExceededException(dacpacSizeInGB, thresholdSizeInGB))
        {
        }

        public MaxDacpacSizeExceededException(string message)
            : base(message)
        {
        }

        public MaxDacpacSizeExceededException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MaxDacpacSizeExceededException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The Source is not a detached tfs database
    /// </summary>
    [Serializable]
    public class SourceIsNotADetachedDatabaseException : DataImportException
    {
        public SourceIsNotADetachedDatabaseException(Exception innerException)
            : base(DataImportResources.SourceIsNotDetachedDatabase(), innerException)
        {
        }

        public SourceIsNotADetachedDatabaseException(string message)
            : base(message)
        {
        }

        public SourceIsNotADetachedDatabaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SourceIsNotADetachedDatabaseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The Source is a configuration tfs database
    /// </summary>
    [Serializable]
    public class SourceIsTFSConfigurationDatabaseException : DataImportException
    {
        public SourceIsTFSConfigurationDatabaseException()
            : base(DataImportResources.SourceIsTFSConfigurationDatabase())
        {
        }

        public SourceIsTFSConfigurationDatabaseException(string message)
            : base(message)
        {
        }

        public SourceIsTFSConfigurationDatabaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SourceIsTFSConfigurationDatabaseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The Source does not have the snapshot tables
    /// </summary>
    [Serializable]
    public class SourceIsMissingSnapshotTablesException : DataImportException
    {
        public SourceIsMissingSnapshotTablesException()
            : base(DataImportResources.SourceDatabaseIsMissingSnapshotTable())
        {
        }

        public SourceIsMissingSnapshotTablesException(string message)
            : base(message)
        {
        }

        public SourceIsMissingSnapshotTablesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SourceIsMissingSnapshotTablesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The Source dacpac contains exported data, this is a BACPAC
    /// </summary>
    [Serializable]
    public class SourceContainsExportedDataException : DataImportException
    {
        public SourceContainsExportedDataException()
            : base(DataImportResources.SourceDatabaseContainsExtractedData())
        {
        }

        public SourceContainsExportedDataException(string message)
            : base(message)
        {
        }

        public SourceContainsExportedDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SourceContainsExportedDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The Source contains an unsupported milestone
    /// </summary>
    [Serializable]
    public class MilestoneNotSupportedException : DataImportException
    {
        public MilestoneNotSupportedException(string message)
            : base(message)
        {
        }

        public MilestoneNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MilestoneNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The dacpac is not a detached tfs database
    /// </summary>
    [Serializable]
    public class UnableToExtractDacpacInformationException : DataImportException
    {
        public UnableToExtractDacpacInformationException(Exception innerException)
            : base(DataImportResources.UnableToExtractDacpacInformation(), innerException)
        {
        }

        public UnableToExtractDacpacInformationException(string message)
            : base(message)
        {
        }

        public UnableToExtractDacpacInformationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UnableToExtractDacpacInformationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The provided package location is not supported
    /// </summary>
    [Serializable]
    public class PackageLocationNotSupportedException : DataImportException
    {
        public PackageLocationNotSupportedException(string message)
            : base(message)
        {
        }

        public PackageLocationNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PackageLocationNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// DacPac or database is empty (schema only)
    /// </summary>
    [Serializable]
    public class EmptyImportSourceException : DataImportException
    {
        public EmptyImportSourceException(string message)
            : base(message)
        {
        }

        public EmptyImportSourceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected EmptyImportSourceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// thrown when the connection string for source Database appears invalid
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "EmptyImportSourceException", "Microsoft.VisualStudio.Services.WebApi.InvalidImportSourceConnectionStringException, Microsoft.VisualStudio.Services.WebApi, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidImportSourceConnectionStringException : DataImportException
    {
        public InvalidImportSourceConnectionStringException(string message)
            : base(message)
        {
        }

        public InvalidImportSourceConnectionStringException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidImportSourceConnectionStringException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// thrown when timing out attempting to connect with the source Database
    /// </summary>
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "EmptyImportSourceException", "Microsoft.VisualStudio.Services.WebApi.ImportSourceConnectionTimeoutException, Microsoft.VisualStudio.Services.WebApi, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ImportSourceConnectionTimeoutException : DataImportException
    {
        public ImportSourceConnectionTimeoutException(string message)
            : base(message)
        {
        }

        public ImportSourceConnectionTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ImportSourceConnectionTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// WIT Step Failed with non recoverable exception
    /// </summary>
    [Serializable]
    public class WITNonRecoverableImportException : DataImportException
    {
        public WITNonRecoverableImportException(string message)
            : base(message)
        {
        }

        public WITNonRecoverableImportException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected WITNonRecoverableImportException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The version of the client is not supported
    /// </summary>
    [Serializable]
    public class DataImportClientVersionNotSupportedException : DataImportException
    {
        public DataImportClientVersionNotSupportedException(string message)
            : base(message)
        {
        }

        public DataImportClientVersionNotSupportedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DataImportClientVersionNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// thrown when the expiration date is set to an invalid value
    /// </summary>
    [Serializable]
    public class InvalidSASKeyExpirationException : DataImportException
    {
        public InvalidSASKeyExpirationException(string message)
            : base(message)
        {
        }

        public InvalidSASKeyExpirationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidSASKeyExpirationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// thrown when the servicing job encounter a failure identifiable as an error in the user's part
    /// </summary>
    [Serializable]
    public class DataImportUserErrorException : DataImportException
    {
        public DataImportUserErrorException(string message)
            : base(message)
        {
            this.MarkAsUserErrorServicingOrchestrationException(message);
        }

        public DataImportUserErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.MarkAsUserErrorServicingOrchestrationException(message);
        }

        protected DataImportUserErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.MarkAsUserErrorServicingOrchestrationException();
        }
    }
}
