# YAML getting started - Secure files

Secure files can be referend by name, for example:

```yaml
queue: Hosted Linux Preview
steps:

# Download the secure file
- task: DownloadSecureFile@1
  name: mySecureFile
  inputs:
    secureFile: sample-secure-file.txt

# Dump the contents
- script: |
    cat $(mySecureFile.secureFilePath)
```

Note, for rename resiliency, secure files can be specified by their GUID instead of name.
