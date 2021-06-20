using System;
using System.Collections.Generic;

namespace GitHub.Runner.Common
{
    //
    // Abstracts away interactions with the terminal which allows:
    // (1) Console writes also go to trace for better context in the trace
    // (2) Reroute in tests
    //
    [ServiceLocator(Default = typeof(Terminal))]
    public interface ITerminal : IRunnerService, IDisposable
    {
        event EventHandler CancelKeyPress;

        bool Silent { get; set; }
        string ReadLine();
        string ReadSecret();
        void Write(string message, ConsoleColor? colorCode = null);
        void WriteLine();
        void WriteLine(string line, ConsoleColor? colorCode = null);
        void WriteError(Exception ex);
        void WriteError(string line);
        void WriteSection(string message);
        void WriteSuccessMessage(string message);
    }

    public sealed class Terminal : RunnerService, ITerminal
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
                HostContext.SecretMasker.AddValue(val);
            }

            Trace.Info($"Read value: '{val}'");
            return val;
        }

        public void Write(string message, ConsoleColor? colorCode = null)
        {
            Trace.Info($"WRITE: {message}");
            if (!Silent)
            {
                if (colorCode != null)
                {
                    Console.ForegroundColor = colorCode.Value;
                    Console.Write(message);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(message);
                }
            }
        }

        public void WriteLine()
        {
            WriteLine(string.Empty);
        }

        // Do not add a format string overload. Terminal messages are user facing and therefore
        // should be localized. Use the Loc method in the StringUtil class.
        public void WriteLine(string line, ConsoleColor? colorCode = null)
        {
            Trace.Info($"WRITE LINE: {line}");
            if (!Silent)
            {
                if (colorCode != null)
                {
                    Console.ForegroundColor = colorCode.Value;
                    Console.WriteLine(line);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine(line);
                }
            }
        }

        public void WriteError(Exception ex)
        {
            Trace.Error("WRITE ERROR (exception):");
            Trace.Error(ex);
            if (!Silent)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }

        // Do not add a format string overload. Terminal messages are user facing and therefore
        // should be localized. Use the Loc method in the StringUtil class.
        public void WriteError(string line)
        {
            Trace.Error($"WRITE ERROR: {line}");
            if (!Silent)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(line);
                Console.ResetColor();
            }
        }

        public void WriteSection(string message)
        {
            if (!Silent)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"# {message}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        public void WriteSuccessMessage(string message)
        {
            if (!Silent)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("√ ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
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
