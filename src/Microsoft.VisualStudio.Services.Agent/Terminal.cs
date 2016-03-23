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
        void WriteError(string line);
    }

    public sealed class Terminal : AgentService, ITerminal
    {
        public bool Silent { get; set; }

        public event EventHandler CancelKeyPress;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            CancelKeyPress?.Invoke(this, e);
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        // TODO: Consider using SecureString.
        public string ReadSecret()
        {
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
                        // TODO: Remove a character from the screen also.
                    }
                }
                else if (key.KeyChar > 0)
                {
                    chars.Add(key.KeyChar);
                    Console.Write("*");
                }
            }

            string val = new String(chars.ToArray());
            Trace.Info("Secret gathered.");
            return val;
        }

        public void Write(string message)
        {
            Trace.Info($"term: {message}");
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
        // should be localized. Use the Loc extension method in the TerminalExtensions class.
        public void WriteLine(string line)
        {
            Trace.Info($"term: {line}");
            if (!Silent)
            {
                Console.WriteLine(line);
            }
        }

        // Do not add a format string overload. Terminal messages are user facing and therefore
        // should be localized. Use the Loc methods from the TerminalExtensions class.
        public void WriteError(string line)
        {
            Trace.Error($"term: {line}");
            if (!Silent)
            {
                Console.Error.WriteLine(line);
            }
        }

        void Dispose(bool disposing)
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