using GitHub.Runner.Common.Util;
using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Logging;
using GitHub.Runner.Sdk;

//
// Pattern:
// cmd1 cmd2 --arg1 arg1val --aflag --arg2 arg2val
//

namespace GitHub.Runner.Common
{
    public sealed class CommandLineParser
    {
        private ISecretMasker _secretMasker;
        private Tracing _trace;

        public List<string> Commands { get; }
        public HashSet<string> Flags { get; }
        public Dictionary<string, string> Args { get; }
        public HashSet<string> SecretArgNames { get; }
        private bool HasArgs { get; set; }

        public CommandLineParser(IHostContext hostContext, string[] secretArgNames)
        {
            _secretMasker = hostContext.SecretMasker;
            _trace = hostContext.GetTrace(nameof(CommandLineParser));

            Commands = new List<string>();
            Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SecretArgNames = new HashSet<string>(secretArgNames ?? new string[0], StringComparer.OrdinalIgnoreCase);
        }

        public bool IsCommand(string name)
        {
            bool result = false;
            if (Commands.Count > 0)
            {
                result = String.Equals(name, Commands[0], StringComparison.CurrentCultureIgnoreCase);
            }

            return result;
        }

        public void Parse(string[] args)
        {
            _trace.Info(nameof(Parse));
            ArgUtil.NotNull(args, nameof(args));
            _trace.Info("Parsing {0} args", args.Length);

            string argScope = null;
            foreach (string arg in args)
            {
                _trace.Info("parsing argument");

                HasArgs = HasArgs || arg.StartsWith("--");
                _trace.Info("HasArgs: {0}", HasArgs);

                if (string.Equals(arg, "/?", StringComparison.Ordinal))
                {
                    Flags.Add("help");
                }
                else if (!HasArgs)
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
                        if (argScope != null && !Args.ContainsKey(argScope = argScope.Trim()))
                        {
                            if (SecretArgNames.Contains(argScope))
                            {
                                _secretMasker.AddValue(arg);
                            }

                            _trace.Info("Adding option '{0}': '{1}'", argScope, arg);
                            // ignore duplicates - first wins - below will be val1
                            // --arg1 val1 --arg1 val1
                            Args.Add(argScope, arg);
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
                        _trace.Info("Ignoring arg");
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
    }
}
