# Step exit-code response protocol (proposal)
Proposed against runner main (8cfa104), with paired runner-container-hooks changes.
Initially tested on runner v2.338.2 and runner-container-hooks v0.8.1.

For run_script_step and run_container_step, the runner sends
supportsStepExitCode: true. A supporting hook distinguishes completed workload
processes from failed hook infrastructure and writes {"exitCode": N} to the
response file, then exits zero. N is an integer 0 through 255. Existing state
fields may coexist. Script hooks return the exact workload exit code through ContainerStepHost to
the action handler, which logs it once and fails the step normally. Container
actions apply the same outcome directly. Existing continue-on-error handling
continues to apply.

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
