#if OS_WINDOWS
using Microsoft.Win32;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(WindowsRegistryManager))]
    public interface IWindowsRegistryManager : IAgentService
    {
        string GetValue(RegistryHive hive, string subKeyName, string name);
        void SetValue(RegistryHive hive, string subKeyName, string name, string value);
        void DeleteValue(RegistryHive hive, string subKeyName, string name);
        bool SubKeyExists(RegistryHive hive, string subKeyName);
    }

    public class WindowsRegistryManager : AgentService, IWindowsRegistryManager
    {
        public void DeleteValue(RegistryHive hive, string subKeyName, string name)
        {            
            using(RegistryKey key = OpenRegistryKey(hive, subKeyName, true))
            {
                if (key != null)
                {
                    key.DeleteValue(name, false);
                }
            }
        }

        public string GetValue(RegistryHive hive, string subKeyName, string name)
        {            
            using(RegistryKey key = OpenRegistryKey(hive, subKeyName, false))
            {
                if(key == null)
                {
                    return null;
                }

                var value = key.GetValue(name, null);
                return value != null ? value.ToString() : null;
            }
        }

        public void SetValue(RegistryHive hive, string subKeyName, string name, string value)
        {
            using(RegistryKey key = OpenRegistryKey(hive, subKeyName, true))
            {
                if(key == null)
                {
                    //today all the subkeys are well defined and exist on the machine. 
                    //having following in the logs is very less likely but good to log such occurances
                    Trace.Warning($"Couldnt get the subkey '{subKeyName}. Will not be able to set the value.");
                    return;
                }

                key.SetValue(name, value);
            }
        }

        public bool SubKeyExists(RegistryHive hive, string subKeyName)
        {
            using(RegistryKey key = OpenRegistryKey(hive, subKeyName, false))
            {
                return key != null;
            }
        }

        private RegistryKey OpenRegistryKey(RegistryHive hive, string subKeyName, bool writable = true)
        {
            RegistryKey key = null;
            switch (hive)
            {
                case RegistryHive.CurrentUser :
                    key = Registry.CurrentUser.OpenSubKey(subKeyName, writable);                    
                    break;
                case RegistryHive.Users :
                    key = Registry.Users.OpenSubKey(subKeyName, writable);
                    break;
                case RegistryHive.LocalMachine:
                    key = Registry.LocalMachine.OpenSubKey(subKeyName, writable);                    
                    break;
            }
            return key;
        }
    }
}
#endif