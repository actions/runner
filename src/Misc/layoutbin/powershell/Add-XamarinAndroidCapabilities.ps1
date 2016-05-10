[CmdletBinding()]
param()

$null = Add-CapabilityFromRegistry -Name 'Xamarin.Android' -Hive 'LocalMachine' -View 'Registry32' -KeyName 'Software\Novell\Mono for Android' -ValueName 'InstalledVersion'
