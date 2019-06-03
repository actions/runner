using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Runner.Common
{
#if OS_WINDOWS
    public static class WindowsProcessExtensions
    {
        // Reference: https://blogs.msdn.microsoft.com/matt_pietrek/2004/08/25/reading-another-processs-environment/
        // Reference: http://blog.gapotchenko.com/eazfuscator.net/reading-environment-variables
        public static string GetEnvironmentVariable(this Process process, IHostContext hostContext, string variable)
        {
            var trace = hostContext.GetTrace(nameof(WindowsProcessExtensions));
            Dictionary<string, string> environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            IntPtr processHandle = process.SafeHandle.DangerousGetHandle();

            IntPtr environmentBlockAddress;
            if (Environment.Is64BitOperatingSystem)
            {
                PROCESS_BASIC_INFORMATION64 pbi = new PROCESS_BASIC_INFORMATION64();
                int returnLength = 0;
                int status = NtQueryInformationProcess64(processHandle, PROCESSINFOCLASS.ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), ref returnLength);
                if (status != 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                bool wow64;
                if (!IsWow64Process(processHandle, out wow64))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!wow64)
                {
                    // 64 bits process on 64 bits OS
                    IntPtr UserProcessParameterAddress = ReadIntPtr64(processHandle, new IntPtr(pbi.PebBaseAddress) + 0x20);
                    environmentBlockAddress = ReadIntPtr64(processHandle, UserProcessParameterAddress + 0x80);
                }
                else
                {
                    // 32 bits process on 64 bits OS
                    IntPtr UserProcessParameterAddress = ReadIntPtr32(processHandle, new IntPtr(pbi.PebBaseAddress) + 0x1010);
                    environmentBlockAddress = ReadIntPtr32(processHandle, UserProcessParameterAddress + 0x48);
                }
            }
            else
            {
                PROCESS_BASIC_INFORMATION32 pbi = new PROCESS_BASIC_INFORMATION32();
                int returnLength = 0;
                int status = NtQueryInformationProcess32(processHandle, PROCESSINFOCLASS.ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), ref returnLength);
                if (status != 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // 32 bits process on 32 bits OS
                IntPtr UserProcessParameterAddress = ReadIntPtr32(processHandle, new IntPtr(pbi.PebBaseAddress) + 0x10);
                environmentBlockAddress = ReadIntPtr32(processHandle, UserProcessParameterAddress + 0x48);
            }

            MEMORY_BASIC_INFORMATION memInfo = new MEMORY_BASIC_INFORMATION();
            if (VirtualQueryEx(processHandle, environmentBlockAddress, ref memInfo, Marshal.SizeOf(memInfo)) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Int64 dataSize = memInfo.RegionSize.ToInt64() - (environmentBlockAddress.ToInt64() - memInfo.BaseAddress.ToInt64());

            byte[] envData = new byte[dataSize];
            IntPtr res_len = IntPtr.Zero;
            if (!ReadProcessMemory(processHandle, environmentBlockAddress, envData, new IntPtr(dataSize), ref res_len))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (res_len.ToInt64() != dataSize)
            {
                throw new ArgumentOutOfRangeException(nameof(ReadProcessMemory));
            }

            string environmentVariableString;
            Int64 environmentVariableBytesLength = 0;
            // check env encoding
            if (envData[0] != 0 && envData[1] == 0)
            {
                // Unicode
                for (Int64 index = 0; index < dataSize; index++)
                {
                    // Unicode encoded environment variables block ends up with '\0\0\0\0'.
                    if (environmentVariableBytesLength == 0 &&
                        envData[index] == 0 &&
                        index + 3 < dataSize &&
                        envData[index + 1] == 0 &&
                        envData[index + 2] == 0 &&
                        envData[index + 3] == 0)
                    {
                        environmentVariableBytesLength = index + 3;
                    }
                    else if (environmentVariableBytesLength != 0)
                    {
                        // set it '\0' so we can easily trim it, most array method doesn't take int64
                        envData[index] = 0;
                    }
                }

                if (environmentVariableBytesLength == 0)
                {
                    throw new ArgumentException(nameof(environmentVariableBytesLength));
                }

                environmentVariableString = Encoding.Unicode.GetString(envData);
            }
            else if (envData[0] != 0 && envData[1] != 0)
            {
                // ANSI
                for (Int64 index = 0; index < dataSize; index++)
                {
                    // Unicode encoded environment variables block ends up with '\0\0'.
                    if (environmentVariableBytesLength == 0 &&
                        envData[index] == 0 &&
                        index + 1 < dataSize &&
                        envData[index + 1] == 0)
                    {
                        environmentVariableBytesLength = index + 1;
                    }
                    else if (environmentVariableBytesLength != 0)
                    {
                        // set it '\0' so we can easily trim it, most array method doesn't take int64
                        envData[index] = 0;
                    }
                }

                if (environmentVariableBytesLength == 0)
                {
                    throw new ArgumentException(nameof(environmentVariableBytesLength));
                }

                environmentVariableString = Encoding.Default.GetString(envData);
            }
            else
            {
                throw new ArgumentException(nameof(envData));
            }

            foreach (var envString in environmentVariableString.Split("\0", StringSplitOptions.RemoveEmptyEntries))
            {
                string[] env = envString.Split("=", 2);
                if (!string.IsNullOrEmpty(env[0]))
                {
                    environmentVariables[env[0]] = env[1];
                    trace.Verbose($"PID:{process.Id} ({env[0]}={env[1]})");
                }
            }

            if (environmentVariables.TryGetValue(variable, out string envVariable))
            {
                return envVariable;
            }
            else
            {
                return null;
            }
        }

        private static IntPtr ReadIntPtr32(IntPtr hProcess, IntPtr ptr)
        {
            IntPtr readPtr = IntPtr.Zero;
            IntPtr data = Marshal.AllocHGlobal(sizeof(Int32));
            try
            {
                IntPtr res_len = IntPtr.Zero;
                if (!ReadProcessMemory(hProcess, ptr, data, new IntPtr(sizeof(Int32)), ref res_len))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (res_len.ToInt32() != sizeof(Int32))
                {
                    throw new ArgumentOutOfRangeException(nameof(ReadProcessMemory));
                }

                readPtr = new IntPtr(Marshal.ReadInt32(data));
            }
            finally
            {
                Marshal.FreeHGlobal(data);
            }

            return readPtr;
        }

        private static IntPtr ReadIntPtr64(IntPtr hProcess, IntPtr ptr)
        {
            IntPtr readPtr = IntPtr.Zero;
            IntPtr data = Marshal.AllocHGlobal(IntPtr.Size);
            try
            {
                IntPtr res_len = IntPtr.Zero;
                if (!ReadProcessMemory(hProcess, ptr, data, new IntPtr(sizeof(Int64)), ref res_len))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (res_len.ToInt32() != IntPtr.Size)
                {
                    throw new ArgumentOutOfRangeException(nameof(ReadProcessMemory));
                }

                readPtr = Marshal.ReadIntPtr(data);
            }
            finally
            {
                Marshal.FreeHGlobal(data);
            }

            return readPtr;
        }

        private enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public int AllocationProtect;
            public IntPtr RegionSize;
            public int State;
            public int Protect;
            public int Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION64
        {
            public long ExitStatus;
            public long PebBaseAddress;
            public long AffinityMask;
            public long BasePriority;
            public long UniqueProcessId;
            public long InheritedFromUniqueProcessId;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION32
        {
            public int ExitStatus;
            public int PebBaseAddress;
            public int AffinityMask;
            public int BasePriority;
            public int UniqueProcessId;
            public int InheritedFromUniqueProcessId;
        };

        [DllImport("ntdll.dll", SetLastError = true, EntryPoint = "NtQueryInformationProcess")]
        private static extern int NtQueryInformationProcess64(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION64 processInformation, int processInformationLength, ref int returnLength);

        [DllImport("ntdll.dll", SetLastError = true, EntryPoint = "NtQueryInformationProcess")]
        private static extern int NtQueryInformationProcess32(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION32 processInformation, int processInformationLength, ref int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr processHandle, out bool wow64Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, IntPtr dwSize, ref IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, IntPtr dwSize, ref IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern int VirtualQueryEx(IntPtr processHandle, IntPtr baseAddress, ref MEMORY_BASIC_INFORMATION memoryInformation, int memoryInformationLength);
    }
#else
    public static class LinuxProcessExtensions
    {
        public static string GetEnvironmentVariable(this Process process, IHostContext hostContext, string variable)
        {
            var trace = hostContext.GetTrace(nameof(LinuxProcessExtensions));
            Dictionary<string, string> env = new Dictionary<string, string>();

            if (Directory.Exists("/proc"))
            {
                string envFile = $"/proc/{process.Id}/environ";
                trace.Info($"Read env from {envFile}");
                string envContent = File.ReadAllText(envFile);
                if (!string.IsNullOrEmpty(envContent))
                {
                    // on linux, environment variables are seprated by '\0'
                    var envList = envContent.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var envStr in envList)
                    {
                        // split on the first '='
                        var keyValuePair = envStr.Split('=', 2);
                        if (keyValuePair.Length == 2)
                        {
                            env[keyValuePair[0]] = keyValuePair[1];
                            trace.Verbose($"PID:{process.Id} ({keyValuePair[0]}={keyValuePair[1]})");
                        }
                    }
                }
            }
            else
            {
                // On OSX, there is no /proc folder for us to read environment for given process,
                // So we have call `ps e -p <pid> -o command` to print out env to STDOUT,
                // However, the output env are not format in a parseable way, it's just a string that concatenate all envs with space,
                // It doesn't escape '=' or ' ', so we can't parse the output into a dictionary of all envs.
                // So we only look for the env you request, in the format of variable=value. (it won't work if you variable contains = or space)
                trace.Info($"Read env from output of `ps e -p {process.Id} -o command`");
                List<string> psOut = new List<string>();
                object outputLock = new object();
                using (var p = hostContext.CreateService<IProcessInvoker>())
                {
                    p.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stdout)
                    {
                        if (!string.IsNullOrEmpty(stdout.Data))
                        {
                            lock (outputLock)
                            {
                                psOut.Add(stdout.Data);
                            }
                        }
                    };

                    p.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stderr)
                    {
                        if (!string.IsNullOrEmpty(stderr.Data))
                        {
                            lock (outputLock)
                            {
                                trace.Error(stderr.Data);
                            }
                        }
                    };

                    int exitCode = p.ExecuteAsync(workingDirectory: hostContext.GetDirectory(WellKnownDirectory.Root),
                                                  fileName: "ps",
                                                  arguments: $"e -p {process.Id} -o command",
                                                  environment: null,
                                                  cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
                    if (exitCode == 0)
                    {
                        trace.Info($"Successfully dump environment variables for {process.Id}");
                        if (psOut.Count > 0)
                        {
                            string psOutputString = string.Join(" ", psOut);
                            trace.Verbose($"ps output: '{psOutputString}'");

                            int varStartIndex = psOutputString.IndexOf(variable, StringComparison.Ordinal);
                            if (varStartIndex >= 0)
                            {
                                string rightPart = psOutputString.Substring(varStartIndex + variable.Length + 1);
                                if (rightPart.IndexOf(' ') > 0)
                                {
                                    string value = rightPart.Substring(0, rightPart.IndexOf(' '));
                                    env[variable] = value;
                                }
                                else
                                {
                                    env[variable] = rightPart;
                                }

                                trace.Verbose($"PID:{process.Id} ({variable}={env[variable]})");
                            }
                        }
                    }
                }
            }

            if (env.TryGetValue(variable, out string envVariable))
            {
                return envVariable;
            }
            else
            {
                return null;
            }
        }
    }
#endif
}
