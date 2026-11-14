# Step exit-code response protocol (proposal)
Based on runner v2.338.2 and paired runner-container-hooks v0.8.1 changes.

For run_script_step and run_container_step, the runner sends
supportsStepExitCode: true. A supporting hook distinguishes completed workload
processes from failed hook infrastructure and writes {"exitCode": N} to the
response file, then exits zero. N is an integer 0 through 255. Existing state
fields may coexist. The runner logs the exact nonzero code and fails the step
normally, allowing existing continue-on-error handling to apply.

A hook crash, transport failure, setup/cleanup failure, malformed Kubernetes
status, or malformed response still fails as infrastructure. An ordinary
workload exit must not erase stdout/stderr or skip file-command synchronization.

Compatibility: new runner + old hooks retains existing behavior (including
old misleading wrapper). New hooks + old runner retains legacy nonzero hook
failure, never reports a failed workload as success. Both sides required for
improved classification. Missing exitCode remains legal for legacy hooks;
explicit invalid/null/string/fractional/out-of-range exitCode is rejected.

No production runner, cluster, image, registration or remote repository changed.
Local regression tests do not establish production cluster behavior or GitHub
UI appearance. Stage with both patched components before rollout.
