function Get-VisualStudio_15_0 {
    [CmdletBinding()]
    param()

    try {
        # Short-circuit if the setup configuration class ID isn't registered.
        if (!(Test-Container -LiteralPath 'REGISTRY::HKEY_CLASSES_ROOT\CLSID\{177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D}')) {
            return
        }

        # If the type has already been loaded once, then it is not loaded again.
        Write-Host "Adding Visual Studio setup helpers."
        Add-Type -Debug:$false -TypeDefinition @'
namespace CapabilityHelpers.VisualStudio.Setup
{
    using System;
    using System.Collections.Generic;
    using CapabilityHelpers.VisualStudio.Setup.Com;

    public sealed class Instance
    {
        public string Description { get; set; }

        public string DisplayName { get; set; }

        public string Id { get; set; }

        public System.Runtime.InteropServices.ComTypes.FILETIME InstallDate { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public Version Version
        {
            get
            {
                try
                {
                    return new Version(VersionString);
                }
                catch (Exception)
                {
                    return new Version(0, 0);
                }
            }
        }

        public string VersionString { get; set; }

        public static List<Instance> GetInstances()
        {
            List<Instance> list = new List<Instance>();
            ISetupConfiguration config = new SetupConfiguration() as ISetupConfiguration;
            IEnumSetupInstances enumInstances = config.EnumInstances();
            ISetupInstance[] instances = new ISetupInstance[1];
            int fetched = 0;
            enumInstances.Next(1, instances, out fetched);
            while (fetched > 0)
            {
                ISetupInstance instance = instances[0];
                list.Add(new Instance()
                {
                    Description = instance.GetDescription(),
                    DisplayName = instance.GetDisplayName(),
                    Id = instance.GetInstanceId(),
                    InstallDate = instance.GetInstallDate(),
                    Name = instance.GetInstallationName(),
                    Path = instance.GetInstallationPath(),
                    VersionString = instance.GetInstallationVersion(),
                });

                enumInstances.Next(1, instances, out fetched);
            }

            return list;
        }
    }
}

namespace CapabilityHelpers.VisualStudio.Setup.Com
{
    using System;
    using System.Runtime.InteropServices;

    [ComImport]
    [Guid("6380BCFF-41D3-4B2E-8B2E-BF8A6810C848")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumSetupInstances
    {
        void Next(
            [In, MarshalAs(UnmanagedType.U4)] int celt,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface)] ISetupInstance[] rgelt,
            [Out, MarshalAs(UnmanagedType.U4)] out int pceltFetched);

        void Skip([In, MarshalAs(UnmanagedType.U4)] int celt);

        void Reset();

        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumSetupInstances Clone();
    }

    [ComImport]
    [Guid("42843719-DB4C-46C2-8E7C-64F1816EFD5B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISetupConfiguration
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumSetupInstances EnumInstances();
    }

    [ComImport]
    [Guid("B41463C3-8866-43B5-BC33-2B0676F7F42E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISetupInstance
    {
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetInstanceId();

        [return: MarshalAs(UnmanagedType.Struct)]
        System.Runtime.InteropServices.ComTypes.FILETIME GetInstallDate();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetInstallationName();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetInstallationPath();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetInstallationVersion();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetDisplayName([In, MarshalAs(UnmanagedType.U4)] int lcid = default(int));

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetDescription([In, MarshalAs(UnmanagedType.U4)] int lcid = default(int));
    }

    [ComImport]
    [Guid("42843719-DB4C-46C2-8E7C-64F1816EFD5B")]
    [CoClass(typeof(SetupConfigurationClass))]
    [TypeLibImportClass(typeof(SetupConfigurationClass))]
    public interface SetupConfiguration : ISetupConfiguration
    {
    }

    [ComImport]
    [Guid("177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D")]
    [ClassInterface(ClassInterfaceType.None)]
    public class SetupConfigurationClass
    {
    }
}
'@
        Write-Host "Getting Visual Studio setup instances."
        $instances = @( [CapabilityHelpers.VisualStudio.Setup.Instance]::GetInstances() )
        Write-Host "Found $($instances.Count) instances."
        Write-Host ($instances | Format-List * | Out-String)
        return $instances |
            Where-Object { $_.Version.Major -eq 15 -and $_.Version.Minor -eq 0 } |
            Sort-Object -Descending -Property Version |
            Select-Object -First 1
    } catch {
        Write-Host ($_ | Out-String)
    }
}
