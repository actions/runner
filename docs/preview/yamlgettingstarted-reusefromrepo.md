# YAML getting started - Reuse from other repositories

Repository resources may be defined in the resources section for use as template source. 
For more details on repository resource support and properties see [repositories](yamlgettingstarted-resources.md#Repositories). 

For example, a team which manages a separate repository for templates may define a definition
in one repository which utilizes a common template for an organization. Much like the task
specification uses `task@version`, an external template reference uses `filePath@repository`. If
no source specification is present the current repository, or the repository hosting the file 
currently being processed, is used as the template source.

```yaml
# File: steps/msbuild.yml    # This is the reusable steps template, which is located in a separate repository.
                             # The repository is described further below, from the entry file .vsts-ci.yml.

parameters:
  solution: '**/*.sln'

steps:
- task: msbuild@1
  inputs:
    solution: ${{ parameters.solution }}
- task: vstest@2
  inputs:
    solution: ${{ parameters.solution }}
```

```yaml
# File: .vsts-ci.yml    # This is the entry file.

resources:
  repositories:
  - repository: templates
    type: github
    endpoint: my-github-endpoint
    name: contoso/build-templates
    ref: refs/tags/lkg
    
steps:
# This file will be pulled from the contoso/build-templates repository
- template: steps/msbuild.yml@templates
  parameters:
    solution: my.sln

# This file will be pulled from the same repository as .vsts-ci.yml    
- template: steps/mstest.yml
```

When the file `.vsts-ci.yml` is processed, the repository resources are loaded. In the case of
a git repository, the `ref` property is used to resolve to a specific commit. Once the version
has been determined, it is saved onto the repository resource as a `version` property. The
resolved version is then used for the duration of the pipeline.
