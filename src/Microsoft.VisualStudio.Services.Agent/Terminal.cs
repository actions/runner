using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Agent
{
    //
    // Abstracts away interactions with the terminal which allows:
    // (1) Console writes also go to trace for better context in the trace
    // (2) Reroute in tests
    //
    [ServiceLocator(Default = typeof(Terminal))]
    public interface ITerminal : IAgentService, IDisposable
    {
        event EventHandler CancelKeyPress;

        bool Silent { get; set; }
        string ReadLine();
        string ReadSecret();
        void Write(string message);
        void WriteLine();
        void WriteLine(string line);
        void WriteError(Exception ex);
        void WriteError(string line);
    }

    public sealed class Terminal : AgentService, ITerminal
    {
        private ISecretMasker _secretMasker;

        public bool Silent { get; set; }

        public event EventHandler CancelKeyPress;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _secretMasker = hostContext.GetService<ISecretMasker>();
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            CancelKeyPress?.Invoke(this, e);
        }

        public string ReadLine()
        {
            // Read and trace the value.
            Trace.Info("READ LINE");
            string value = Console.ReadLine();
            Trace.Info($"Read value: '{value}'");
            return value;
        }

        // TODO: Consider using SecureString.
        public string ReadSecret()
        {
            Trace.Info("READ SECRET");
            var chars = new List<char>();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (chars.Count > 0)
                    {
                        chars.RemoveAt(chars.Count - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.KeyChar > 0)
                {
                    chars.Add(key.KeyChar);
                    Console.Write("*");
                }
            }

            // Trace whether a value was entered.
            string val = new String(chars.ToArray());
            if (!string.IsNullOrEmpty(val))
            {
                _secretMasker.AddValue(val);
            }

            Trace.Info($"Read value: '{val}'");
            return val;
        }

        public void Write(string message)
        {
            Trace.Info($"WRITE: {message}");
            if (!Silent)
            {
                Console.Write(message);
            }
        }

        public void WriteLine()
        {
            WriteLine(string.Empty);
        }

        // Do not add a format string overload. Terminal messages are user facing and therefore
        // should be localized. Use the Loc method in the StringUtil class.
        public void WriteLine(string line)
        {
            Trace.Info($"WRITE LINE: {line}");
            if (!Silent)
            {
                Console.WriteLine(line);
            }
        }

        public void WriteError(Exception ex)
        {
            Trace.Error("WRITE ERROR (exception):");
            Trace.Error(ex);
            if (!Silent)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        // Do not add a format string overload. Terminal messages are user facing and therefore
        // should be localized. Use the Loc method in the StringUtil class.
        public void WriteError(string line)
        {
            Trace.Error($"WRITE ERROR: {line}");
            if (!Silent)
            {
                Console.Error.WriteLine(line);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.CancelKeyPress -= Console_CancelKeyPress;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}