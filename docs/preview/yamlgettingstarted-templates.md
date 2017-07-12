# YAML getting started - Process templates (internal only, public preview soon)

Process templates enable portions of a process to be imported from other files. Parameters can be
passed to templates, and [mustache preprocessing](yamlgettingstarted-mustache.md) can be leveraged
within the template to consume the parameters.

## Deserialization

At a high level, the deserialization process is:

1. Preprocess file (mustache)
1. Deserialize yaml
1. If structure references a template
 1. Preprocess template (mustache)
 1. Deserialize yaml
 1. If structure references a template, recursively repeat 3.i
 1. Merge structure into caller structure

### Template parameters

When a template is referenced, the caller may pass parameters to the template. The parameters are
overlaid onto any user defined context in the target file (YAML front matter). The overlaid object
is used as the mustache context during template deserialization.

Default parameter values can be specified in the template's YAML front matter. Since the caller-defined
parameter values are overlaid on top, any parameters that are not specified will not be overridden.

TODO: What about the server generated context? Should that always be available in the mustache context
during template deserialization without the caller explicitly passing it in? Should all outer root context?

TODO: EXAMPLES

### Template granularity

Templates may be used to define an entire process, or may be used to pull in smaller pieces.

The following types of templates are supported:
- entire process
- array of phases
- array of jobs
- array of variables
- array of steps

TODO: MORE DETAILS ABOUT HOW ARRAYS ARE PULLED IN, MULTIPLE ARRAYS CAN BE PULLED INTO SINGLE OUTER ARRAY

TODO: EXAMPLES

### Template chaining

Templates may reference other templates, but only at lower level objects in the hierarchy.

For example, a process template can reference a phases template. A process template cannot reference another process template.

### TODO: Discuss overrides and selectors
