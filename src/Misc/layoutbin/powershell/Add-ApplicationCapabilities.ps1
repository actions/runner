[CmdletBinding()]
param()

Add-CapabilityFromApplication -Name 'npm' -ApplicationName 'npm'
Add-CapabilityFromApplication -Name 'gulp' -ApplicationName 'gulp'
Add-CapabilityFromApplication -Name 'node.js' -ApplicationName 'node'
Add-CapabilityFromApplication -Name 'bower' -ApplicationName 'bower'
Add-CapabilityFromApplication -Name 'grunt' -ApplicationName 'grunt'
Add-CapabilityFromApplication -Name 'svn' -ApplicationName 'svn'
Add-CapabilityFromApplication -Name 'cmake' -ApplicationName 'cmake'
Add-CapabilityFromApplication -Name 'docker' -ApplicationName 'docker'
