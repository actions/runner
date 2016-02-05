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
                catch (System.Exception ex)
                {
                    message = String.Format(CultureInfo.InvariantCulture,"{0} {1}", format, String.Join(", ", args));
                }
            }
            
            return message;
        }
    }
}