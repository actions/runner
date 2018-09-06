# Having trouble with resource authorization?

Symptom: You've added a new service endpoint (or other resource). Now, builds fail with an error message about resource authorization.

Fix: Follow these steps.

- Navigate to the build definition in the web
- Switch the default branch to your branch that includes the new service endpoint reference
- Save the definition
- Revert back to your desired settings
- Save the definition again

## Why does this fix it?
The act of saving the definition loads the file (from the default branch) and authorizes discovered resources.
We are working on a better experience which won't require these steps.

Learn more about [YAML resource authorization](yamlgettingstarted-authz.md).
