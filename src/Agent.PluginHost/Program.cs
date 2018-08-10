using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using Agent.Sdk;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Agent.PluginHost
{
    public static class Program
    {
        private static CancellationTokenSource tokenSource = new CancellationTokenSource();
        private static string executingAssemblyLocation = string.Empty;

        public static int Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            try
            {
                ArgUtil.NotNull(args, nameof(args));
                ArgUtil.Equal(2, args.Length, nameof(args.Length));

                string pluginType = args[0];
                if (string.Equals("task", pluginType, StringComparison.OrdinalIgnoreCase))
                {
                    string assemblyQualifiedName = args[1];
                    ArgUtil.NotNullOrEmpty(assemblyQualifiedName, nameof(assemblyQualifiedName));

                    // Set encoding to UTF8, process invoker will use UTF8 write to STDIN
                    Console.InputEncoding = Encoding.UTF8;
                    string serializedContext = Console.ReadLine();
                    ArgUtil.NotNullOrEmpty(serializedContext, nameof(serializedContext));

                    AgentTaskPluginExecutionContext executionContext = StringUtil.ConvertFromJson<AgentTaskPluginExecutionContext>(serializedContext);
                    ArgUtil.NotNull(executionContext, nameof(executionContext));

                    AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                    try
                    {
                        Type type = Type.GetType(assemblyQualifiedName, throwOnError: true);
                        var taskPlugin = Activator.CreateInstance(type) as IAgentTaskPlugin;
                        ArgUtil.NotNull(taskPlugin, nameof(taskPlugin));
                        taskPlugin.RunAsync(executionContext, tokenSource.Token).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        // any exception throw from plugin will fail the task.
                        executionContext.Error(ex.ToString());
                    }
                    finally
                    {
                        AssemblyLoadContext.Default.Resolving -= ResolveAssembly;
                    }

                    return 0;
                }
                else if (string.Equals("command", pluginType, StringComparison.OrdinalIgnoreCase))
                {
                    string assemblyQualifiedName = args[1];
                    ArgUtil.NotNullOrEmpty(assemblyQualifiedName, nameof(assemblyQualifiedName));

                    string serializedContext = Console.ReadLine();
                    ArgUtil.NotNullOrEmpty(serializedContext, nameof(serializedContext));

                    AgentCommandPluginExecutionContext executionContext = StringUtil.ConvertFromJson<AgentCommandPluginExecutionContext>(serializedContext);
                    ArgUtil.NotNull(executionContext, nameof(executionContext));

                    AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                    try
                    {
                        Type type = Type.GetType(assemblyQualifiedName, throwOnError: true);
                        var commandPlugin = Activator.CreateInstance(type) as IAgentCommandPlugin;
                        ArgUtil.NotNull(commandPlugin, nameof(commandPlugin));
                        commandPlugin.ProcessCommandAsync(executionContext, tokenSource.Token).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        // any exception throw from plugin will fail the command.
                        executionContext.Error(ex.ToString());
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
