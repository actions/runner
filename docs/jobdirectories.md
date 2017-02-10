
# Job directories

## Overview

## Work Folder Layout

The agent keeps working files and folders in a directory named _work under the agent by default but can be configured for another location.

This is available to scripts and tasks as:

```
Variable: Agent.WorkFolder
EnvVar:   AGENT_WORKFOLDER
```

*IMPORTANT*: Variables should always be used to locate a specific directory.  Do not hard code assumptions about the layout as it may change.

## Build

Build maintains source files from the source control systems for building.

Example layout:

```
_work
│
├───1
│   ├───a
│   ├───b
│   ├───s
│   └───TestResults
├───2
│   ├───a
│   ├───b
│   ├───s
│   └───TestResults
└───SourceRootMapping
    │   Mappings.json
    ├───7ca83873-9ab2-43be-86ac-bfb844bf5232
    │   ├───11
    │   │       SourceFolder.json
    │   └───7
    │           SourceFolder.json
    └───GC
```

Each repository is an in incrementing int folder.

### _work/\<#\>

Each definition gets it's own build directory. The build directory is the number directory above the sources and artifacts.  This is typically used if you want to create folders and work outside of the repo sources to avoid local uncommitted changes.

```
Variable: Agent.BuildDirectory
EnvVar:   AGENT_BUILDDIRECTORY
```

Under that folder is ...

### s: source folder

The source repository is downloaded to this folder. This is the root of the repository.

```
Variable: Build.SourcesDirectory
EnvVar:   BUILD_SOURCESDIRECTORY
```

### b: binaries

The binaries folder is useful as an output folder for building outside of the source repo folders.

```
Variable: build.binariesdirectory
EnvVar:   BUILD_BINARIESDIRECTORY
```

### a: artifacts

Copying files to this directory

```
Variable: Build.ArtifactStagingDirectory
EnvVar:   BUILD_ARTIFACTSTAGINGDIRECTORY
```

### TestResults

```
Variable: Common.TestResultsDirectory
EnvVar:   COMMON_TESTRESULTSDIRECTORY
```

## Source Mappings

Location of sources is maintained in the SourceRootMapping folder directly under the root folder.  These files are used by the agent to define the variables that tasks and build scripts use.

*IMPORTANT*: Do not directly access these files or manipulate them.  Use variables.

SourceRootMapping layout:

```
└───SourceRootMapping
    │   Mappings.json
    ├───7ca83873-9ab2-43be-86ac-bfb844bf5232
    │   ├───11
    │   │       SourceFolder.json
    │   └───7
    │           SourceFolder.json
    └───GC
```

### Mappings.json

This maintains an incrementing counter for source folder creation.  This is incremented when a new repository is encountered.

```
{
  "lastBuildFolderCreatedOn": "09/15/2015 00:44:53 -04:00",
  "lastBuildFolderNumber": 4
}
```

### SourceFolder.json

Detailed information about each build working folder is kept in a SourcesFolder.json file.  It is stored under the collectionId (guid) and definitionId (int) folder.

Locations are stored as relative paths relative to the root of the working folder.  This allows for (1) moving of a working folder without rewriting and (2) changing the layout scheme without forcing sources to get pulled unnecessarily.

```
{
  "build_artifactstagingdirectory": "4\\a",
  "agent_builddirectory": "4",
  "collectionName": "DefaultCollection",
  "definitionName": "M87_PrintEnvVars",
  "fileFormatVersion": 2,
  "lastRunOn": "09/15/2015 00:44:53 -04:00",
  "build_sourcesdirectory": "4\\s",
  "common_testresultsdirectory": "4\\TestResults",
  "collectionId": "7ca83873-9ab2-43be-86ac-bfb844bf5232",
  "definitionId": "7",
  "hashKey": "88255a024f3b92da0b6939a240b3b1c3e65e30c7",
  "repositoryUrl": "http://sample.visualstudio.com/DefaultCollection/gitTest/_git/gitTest%20WithSpace",
  "system": "build"
}
```

**collectionName/definitionName**: These are informational fields.  They are useful if you want to locate during troubleshootng to find out where sources are for a given definition.  Searching under the SourceRootMapping folder makes it easy to find.

**hashKey**: elements of the repository details (for example url of the git repo) are used to create a sha1 hash.  Getting a new hashKey indicates that key repository details from the definition have changed enough to warrant pulling a new sources working folder.

### GC

If a definitions repository information changes causing a new build working folder to be created, the old SourceFolder.json will get copied to the GC folder indicating it can deleted and reclaimed.  A tool will be available to iterate and clean up unused working folders.
