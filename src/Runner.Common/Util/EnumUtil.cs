namespace GitHub.Runner.Common.Util
{
    using System;

    public static class EnumUtil
    {
        public static T? TryParse<T>(string value) where T: struct
        {
            T val;
            if (Enum.TryParse(value ?? string.Empty, ignoreCase: true, result: out val))
            {
                return val;
            }

            return null;
        }
    }
}
