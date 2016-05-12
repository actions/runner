[CmdletBinding()]
param()

foreach ($view in @('Registry64', 'Registry32')) {
    if ((Add-CapabilityFromRegistry -Name 'SCVMMAdminConsole' -Hive 'LocalMachine' -View $view -KeyName 'Software\Microsoft\Microsoft System Center Virtual Machine Manager Administrator Console\Setup' -ValueName 'InstallPath')) {
        break
    }
}
