using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

//
// Pattern:
// cmd1 cmd2 --arg1 arg1val --aflag --arg2 arg2val
//

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class CommandLineParser
    {
        private TraceSource _trace;
        private static List<String> validCommands = new List<string> { "configure", "unconfigure", "run", "help", "version" };
        public CommandLineParser(IHostContext hostContext)
        {
            _trace = hostContext.GetTrace("CommandLineParser");

            Commands = new List<string>();
            Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public List<string> Commands { get; }
        public HashSet<string> Flags { get; }
        public Dictionary<string, string> Args { get; }
        public bool HasArgs { get; private set; }

        public bool IsCommand(string name)
        {
            return IsCommand(0, name);
        }

        public bool IsCommand(int index, string name) 
        {
            bool result = false;

            if (Commands.Count > index)
            {
                result = String.Equals(name, Commands[index], StringComparison.CurrentCultureIgnoreCase);
            }

            return result;
        }

        public void Parse(String[] args)
        {
            _trace.Info("Parse()");

            if(args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _trace.Info("Parse {0} args", args.Length);

            if (args.Length == 0)
            {
                _trace.Info("No args");
                return;
            }

            string argScope = null;
            foreach (string arg in args)
            {
                _trace.Info("parsing argument");
                _trace.Info("arg: {0}", arg);

                HasArgs = HasArgs || arg.StartsWith("--");
                _trace.Info("HasArgs: {0}", HasArgs);

                if (!HasArgs)
                {
                    _trace.Info("Adding Command: {0}", arg);
                    Commands.Add(arg.Trim());
                }
                else
                {
                    // it's either an arg, an arg value or a flag
                    if (arg.StartsWith("--") && arg.Length > 2)
                    {
                        string argVal = arg.Substring(2);
                        _trace.Info("arg: {0}", argVal);

                        // this means two --args in a row which means previous was a flag
                        if (argScope != null) 
                        {
                            _trace.Info("Adding flag: {0}", argScope);
                            Flags.Add(argScope.Trim());
                        }

                        argScope = argVal;
                    }
                    else if (!arg.StartsWith("-"))
                    {
                        // we found a value - check if we're in scope of an arg
                        if (argScope != null && !Args.ContainsKey(argScope))
                        {
                            _trace.Info("Adding option {0} value: {1}", argScope, arg);
                            // ignore duplicates - first wins - below will be val1
                            // --arg1 val1 --arg1 val1
                            Args.Add(argScope.Trim(), arg);
                            argScope = null; 
                        }
                    }
                    else
                    {
                        //
                        // ignoring the second value for an arg (val2 below) 
                        // --arg val1 val2

                        // ignoring invalid things like empty - and --
                        // --arg val1 -- --flag
                        _trace.Info("Ignoring: {0}", arg);
                    }
                }
            }
            _trace.Verbose("done parsing arguments");

            // handle last arg being a flag
            if (argScope != null) 
            {
                Flags.Add(argScope);
            }
            _trace.Verbose("Exiting parse");
        }

        public Boolean HasValidCommand()
        {
            return this.Commands.Any(x => validCommands.Contains(x));
        }
    }
}
