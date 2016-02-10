using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public static class Resources
    {
        public static String GetString(String key)
        {
            return key;
        }

        public static String GetString(String key, params Object[] args)
        {
            return String.Concat(key, String.Join(" ", args));
        }
    }
}