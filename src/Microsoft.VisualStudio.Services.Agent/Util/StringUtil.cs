using System;
using System.Globalization;


namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class StringUtil
    {
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
    }
}