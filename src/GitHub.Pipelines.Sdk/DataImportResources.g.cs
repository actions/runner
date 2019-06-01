using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class DataImportResources
    {
        public static string SourceIsNotDetachedDatabase(params object[] args)
        {
            const string Format = @"VS403250: The dacpac or source database is not a detached Azure DevOps Server Collection database. Please refer to the troubleshooting documentation for more details; https://aka.ms/AzureDevOpsImportTroubleshooting";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ImportInvalidSourceExtendedPropertyValue(params object[] args)
        {
            const string Format = @"The dacpac or source database contains an extended property with an invalid value: Name:'{0}', Expected Value:{1}, Actual Value:{2}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MissingSourceExtendedProperty(params object[] args)
        {
            const string Format = @"The dacpac or database does not contain the following extended properties:{0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnsupportedCollectionMilestone(params object[] args)
        {
            const string Format = @"VS403265: The collection's Azure DevOps Server milestone is not supported by the data migration tool: {0}. Please upgrade your Azure DevOps Server to one of the supported versions. The data migration guide has the latest supported versions: https://aka.ms/AzureDevOpsImport";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SourceIsTFSConfigurationDatabase(params object[] args)
        {
            const string Format = @"VS403286: The dacpac or source database is from an Azure DevOps Server Configuration database. You must use a detached Azure DevOps Server collection database. Please refer to the troubleshooting documentation for more details; https://aka.ms/AzureDevOpsImportTroubleshooting";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnableToExtractDacpacInformation(params object[] args)
        {
            const string Format = @"VS403301: An unexpected error was encountered while attempting to read the dacpac. Please check the logs for more information and refer to https://aka.ms/AzureDevOpsImportTroubleshooting";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SourceDatabaseIsMissingSnapshotTable(params object[] args)
        {
            const string Format = @"VS403351: The dacpac or source database is missing an expected table. It's possible that the database was not correctly detached from Azure DevOps Server. See our troubleshooting documentation for more details https://aka.ms/AzureDevOpsImportTroubleshooting";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TfsMigratorVersionIsNotSupported(params object[] args)
        {
            const string Format = @"VS403375: The version of the data migration tool you are using is no longer supported. Please download the latest version from https://aka.ms/DownloadAzureDevOpsMigrator";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TfsMigratorVersionIsNotSupportedForPepare(params object[] args)
        {
            const string Format = @"VS403393: The version of the data migration tool you are using is no longer supported. Please download the latest version from https://aka.ms/DownloadAzureDevOpsMigrator and regenerate the import specification file.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TfsMigratorVersionIsNotSupportedForImport(params object[] args)
        {
            const string Format = @"VS403394: The version of the data migration tool you are using is no longer supported to queue an import. Please download the latest version from https://aka.ms/DownloadAzureDevOpsMigrator and try to queue your import again. There is no need to regenerate the import specification file.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SourceDatabaseContainsExtractedData(params object[] args)
        {
            const string Format = @"VS403386: The specified dacpac file is actually a BACPAC file. BACPACs are not supported for importing a collection into Azure DevOps. See our documentation for generating a dacpac for more details https://aka.ms/CreateAzureDevOpsDACPAC";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SqlPackageVersionNotSupportedException(params object[] args)
        {
            const string Format = @"VS403401: The version of SqlPackage.exe used to create your dacpac is not supported. Please download the latest version from https://aka.ms/ImportSQLPackage and recreate the dacpac.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MaxDacpacSizeExceededException(params object[] args)
        {
            const string Format = @"VS403402: The size of the provided dacpac is {0} GB, exceeding the maximum allowable size of {1} GB. Your collection will need to be imported using the large collection import method - https://aka.ms/AzureDevOpsImportLargeCollection";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
