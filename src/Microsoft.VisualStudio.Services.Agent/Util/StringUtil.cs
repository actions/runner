using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class StringUtil
    {
        private static Dictionary<string, Object> _locStrings;
         
        public static String Format(String format, params Object[] args)
        {
            if (String.IsNullOrEmpty(format))
            {
                return String.Empty;
            }
            
            String message = format;
            
            if(args != null && args.Length > 0)
            {
                try
                {
                    message = String.Format(CultureInfo.InvariantCulture, format, args);
                }
                catch (System.Exception)
                {
                    // TODO: Log that string format failed. Consider moving this into a context base class if that's the only place it's used. Then the current trace scope would be available as well.
                    message = String.Format(CultureInfo.InvariantCulture,"{0} {1}", format, String.Join(", ", args));
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
                    Console.WriteLine(t.ToString());
                    
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