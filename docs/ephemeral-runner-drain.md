# Cooperatively stopping an ephemeral runner

An external supervisor can request drain through either of two opt-in inputs:

- On Linux and macOS, start `./run.sh --drain-on-sigusr1`, then send `SIGUSR1`
  to the `Runner.Listener` PID (not the shell/service wrapper).
- On any supported platform, start `Runner.Listener run --drain-file PATH`, then
  create that file. There is no default sentinel path; relative paths resolve
  against the listener's startup working directory. The listener only reads it.

Both inputs latch the same drain state for the lifetime of the listener process.
They can be enabled together. These options require a runner configured with
`--ephemeral`; persistent runners and SIGINT/SIGTERM behavior are unchanged.

For a read-only container, the signal input requires no writable control file.
Alternatively, put the sentinel on a separately mounted runtime directory; the
installation directory does not need to be writable for the drain mechanism.

For paths containing spaces, invoke the listener directly with a quoted argument,
for example `./bin/Runner.Listener run --drain-file '/run/runner/drain file'`
(use `Runner.Listener.exe` on Windows). The shell wrappers do not preserve such
arguments. A missing or empty path is an error, not an implicit disable switch.

The listener finishes its outstanding message poll. If the response contains a
job, it completes acknowledgement/acquisition and runs that job normally. If
the response is empty, it exits successfully without starting another poll.
An already running job finishes through the existing one-job completion path;
normal job-cancellation messages continue to be processed.

The supervisor must wait for the listener to exit before stopping the host. It
must not immediately follow a drain request with a termination signal.
Idle drain can take the duration of the outstanding long poll, and network
retries can extend that wait.

Signal handlers are installed before session startup. Do not send SIGUSR1 until
the opted-in listener is ready (for example, after `Listening for Jobs`); a
signal sent before handler installation retains its default OS behavior. A
signal request does not persist across process exits. The supervisor must stop
restarting the listener during host shutdown.

The supervisor owns the optional sentinel: use a unique path per listener, and
remove it before deliberately restarting. The runner does not remove it at
startup or write to it. Removing it after the runner has observed it does not
withdraw a drain request. A pre-existing file also supports requesting drain
before the listener starts.

Do not use the service wrapper's stop operation as the drain request: it sends
SIGINT and can force-kill the listener after 30 seconds. Send SIGUSR1 directly
to the opted-in listener, wait for exit, and only then stop the container/host.

An idle drain stops the listener session, but does not deregister the runner or
delete its local credentials. A supervisor can clear any sentinel and reuse
that configuration, or separately remove the registration/configuration before
registering again. Normal automatic deregistration still applies after an
ephemeral runner completes its job.

This is a cooperative idle-stop primitive, not a guarantee about ambiguous
server-side delivery after network failures or about forced host termination.
