# Cooperatively stopping an ephemeral runner

An external supervisor can request drain by creating `.drain` in the runner's
installation directory. This applies only to runners configured with
`--ephemeral`; it does not change persistent runners.

The listener finishes its outstanding message poll. If the response contains a
job, it completes acknowledgement/acquisition and runs that job normally. If
the response is empty, it exits successfully without starting another poll.
An already running job finishes through the existing one-job completion path;
normal job-cancellation messages continue to be processed.

The supervisor must wait for the listener to exit before stopping the host. It
must not immediately follow creation of `.drain` with a termination signal.
Idle drain can take the duration of the outstanding long poll, and network
retries can extend that wait.

The supervisor owns the sentinel: remove it before deliberately starting a new
registration. The runner does not remove it at startup, because doing so could
discard a shutdown request made before the listener begins polling. Each runner
installation has its own sentinel.

An idle drain stops the listener session, but does not deregister the runner or
delete its local credentials. A supervisor can remove the sentinel and reuse
that configuration, or separately remove the registration/configuration before
registering again. Normal automatic deregistration still applies after an
ephemeral runner completes its job.

This is a cooperative idle-stop primitive, not a guarantee about ambiguous
server-side delivery after network failures or about forced host termination.
