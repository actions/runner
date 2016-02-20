namespace Microsoft.VisualStudio.Services.Agent.Util
{
    using System;

    public static class EnumConvertor
    {
        public static T ConvertToEnum<T>(this string value, T defaultValue) where T: struct
        {
            T result;
            return Enum.TryParse<T>(value, true, out result) ? result : defaultValue;
        }

        public static T ConvertToEnum<T>(this string value) where T : struct
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}