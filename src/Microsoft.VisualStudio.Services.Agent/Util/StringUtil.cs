using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class StringUtil
    {
        private static Dictionary<string, Object> _locStrings;

        // TODO: Add unit tests for this. Test cases where args is null and empty array.
        public static string Format(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }

            string message = format;
            if (args != null && args.Length > 0)
            {
                try
                {
                    message = string.Format(CultureInfo.InvariantCulture, format, args);
                }
                catch (Exception)
                {
                    // TODO: Log that string format failed. Consider moving this into a context base class if that's the only place it's used. Then the current trace scope would be available as well.
                    message = string.Format(CultureInfo.InvariantCulture, "{0} {1}", format, string.Join(", ", args));
                }
            }

            return message;
        }

        public static string Loc(string locKey, params Object[] args)
        {
            //
            // TODO: Replace this custom little loc impl with proper one after confirming OSX/Linux support
            //
            string locStr = locKey;
            
            try
            {
                EnsureLoaded();

                if (_locStrings.ContainsKey(locKey))
                {                
                    Object item = _locStrings[locKey];
                    
                    Type t = item.GetType();
                    
                    if (t == typeof(string))
                    {
                        string str = _locStrings[locKey].ToString();
                        locStr = StringUtil.Format(str, args);                            
                    }
                    else if (t == typeof(JArray))
                    {
                        string[] lines = ((JArray)item).ToObject<string[]>();
                        StringBuilder sb = new StringBuilder();
                        foreach (string line in lines)
                        {
                            sb.Append(line);
                            sb.Append(Environment.NewLine);
                            //locStr += (line + Environment.NewLine);
                        }
                        locStr = sb.ToString();
                    }
                }
                else
                {
                    locStr = StringUtil.Format("notFound:{0}", locKey);
                }              
            }
            catch (Exception)
            {
                // loc strings shouldn't take down agent.  any failures returns loc key
            }
            
            return locStr;
        }
                
        private static void EnsureLoaded()
        {
            if (_locStrings == null)
            {   
                string stringsPath = Path.Combine(IOUtil.GetBinPath(), 
                                                CultureInfo.CurrentCulture.Name,
                                                "strings.json");
                                                
                _locStrings = IOUtil.LoadObject<Dictionary<string, Object>>(stringsPath);
            }            
        }        
    }
}