using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Single public facade for the Debug Adapter Protocol subsystem.
    /// Owns the DapServer and DapDebugSession internally; external callers
    /// (JobRunner, StepsRunner) interact only with this class.
    /// </summary>
    public sealed class DapDebugger : RunnerService, IDapDebugger
    {
        private const int DefaultPort = 4711;
        private const string PortEnvironmentVariable = "ACTIONS_RUNNER_DAP_PORT";

        private IDapServer _server;
        private IDapDebugSession _session;
        private CancellationTokenRegistration? _cancellationRegistration;
        private volatile bool _started;

        public bool IsActive => _session?.IsActive == true;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            Trace.Info("DapDebugger initialized");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var port = ResolvePort();

            _server = HostContext.GetService<IDapServer>();
            _session = HostContext.GetService<IDapDebugSession>();

            _server.SetSession(_session);
            _session.SetDapServer(_server);

            await _server.StartAsync(port, cancellationToken);
            _started = true;

            Trace.Info($"DAP debugger started on port {port}");
        }

        public async Task WaitUntilReadyAsync(CancellationToken cancellationToken)
        {
            if (!_started || _server == null || _session == null)
            {
                return;
            }

            Trace.Info("Waiting for debugger client connection...");
            await _server.WaitForConnectionAsync(cancellationToken);
            Trace.Info("Debugger client connected.");

            await _session.WaitForHandshakeAsync(cancellationToken);
            Trace.Info("DAP handshake complete.");

            _cancellationRegistration = cancellationToken.Register(() =>
            {
                Trace.Info("Job cancellation requested, cancelling debug session.");
                _session.CancelSession();
            });
        }

        public async Task StopAsync()
        {
            if (_cancellationRegistration.HasValue)
            {
                _cancellationRegistration.Value.Dispose();
                _cancellationRegistration = null;
            }

            if (_server != null && _started)
            {
                try
                {
                    Trace.Info("Stopping DAP debugger");
                    await _server.StopAsync();
                }
                catch (Exception ex)
                {
                    Trace.Error("Error stopping DAP debugger");
                    Trace.Error(ex);
                }
            }

            _started = false;
        }

        public void CancelSession()
        {
            _session?.CancelSession();
        }

        public async Task OnStepStartingAsync(IStep step, IExecutionContext jobContext, bool isFirstStep, CancellationToken cancellationToken)
        {
            if (!IsActive)
            {
                return;
            }

            try
            {
                await _session.OnStepStartingAsync(step, jobContext, isFirstStep, cancellationToken);
            }
            catch (Exception ex)
            {
                Trace.Warning($"DAP OnStepStarting error: {ex.Message}");
            }
        }

        public void OnStepCompleted(IStep step)
        {
            if (!IsActive)
            {
                return;
            }

            try
            {
                _session.OnStepCompleted(step);
            }
            catch (Exception ex)
            {
                Trace.Warning($"DAP OnStepCompleted error: {ex.Message}");
            }
        }

        public void OnJobCompleted()
        {
            if (!IsActive)
            {
                return;
            }

            try
            {
                _session.OnJobCompleted();
            }
            catch (Exception ex)
            {
                Trace.Warning($"DAP OnJobCompleted error: {ex.Message}");
            }
        }

        private int ResolvePort()
        {
            var portEnv = Environment.GetEnvironmentVariable(PortEnvironmentVariable);
            if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out var customPort) && customPort > 1024 && customPort <= 65535)
            {
                Trace.Info($"Using custom DAP port {customPort} from {PortEnvironmentVariable}");
                return customPort;
            }

            return DefaultPort;
        }
    }
}
