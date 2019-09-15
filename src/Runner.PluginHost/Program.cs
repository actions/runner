using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.PluginHost
{
    public static class Program
    {
        private static CancellationTokenSource tokenSource = new CancellationTokenSource();
        private static string executingAssemblyLocation = string.Empty;

        public static int Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            // Set encoding to UTF8, process invoker will use UTF8 write to STDIN
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                ArgUtil.NotNull(args, nameof(args));
                ArgUtil.Equal(2, args.Length, nameof(args.Length));

                string pluginType = args[0];
                if (string.Equals("action", pluginType, StringComparison.OrdinalIgnoreCase))
                {
                    string assemblyQualifiedName = args[1];
                    ArgUtil.NotNullOrEmpty(assemblyQualifiedName, nameof(assemblyQualifiedName));

                    string serializedContext = Console.ReadLine();
                    ArgUtil.NotNullOrEmpty(serializedContext, nameof(serializedContext));

                    RunnerActionPluginExecutionContext executionContext = StringUtil.ConvertFromJson<RunnerActionPluginExecutionContext>(serializedContext);
                    ArgUtil.NotNull(executionContext, nameof(executionContext));

                    VariableValue culture;
                    ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));
                    if (executionContext.Variables.TryGetValue("system.culture", out culture) &&
                        !string.IsNullOrEmpty(culture?.Value))
                    {
                        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(culture.Value);
                        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture.Value);
                    }

                    AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                    try
                    {
                        Type type = Type.GetType(assemblyQualifiedName, throwOnError: true);
                        var taskPlugin = Activator.CreateInstance(type) as IRunnerActionPlugin;
                        ArgUtil.NotNull(taskPlugin, nameof(taskPlugin));
                        taskPlugin.RunAsync(executionContext, tokenSource.Token).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        // any exception throw from plugin will fail the task.
                        executionContext.Error(ex.Message);
                        executionContext.Debug(ex.StackTrace);
                        return 1;
                    }
                    finally
                    {
                        AssemblyLoadContext.Default.Resolving -= ResolveAssembly;
                    }

                    return 0;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(pluginType);
                }
            }
            catch (Exception ex)
            {
                // infrastructure failure.
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            finally
            {
                Console.CancelKeyPress -= Console_CancelKeyPress;
            }
        }

        private static Assembly ResolveAssembly(AssemblyLoadContext context, AssemblyName assembly)
        {
            string assemblyFilename = assembly.Name + ".dll";
            if (string.IsNullOrEmpty(executingAssemblyLocation))
            {
                executingAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            return context.LoadFromAssemblyPath(Path.Combine(executingAssemblyLocation, assemblyFilename));
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            tokenSource.Cancel();
        }
    }
}
