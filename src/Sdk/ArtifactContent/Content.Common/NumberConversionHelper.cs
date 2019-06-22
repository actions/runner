using System;

namespace GitHub.Services.Content.Common
{
    public static class NumberConversionHelper
    {
        /// <summary>
        /// Uses IEC definition of a megabyte, bytes*1000² (SI units). We went with this direction rather than the Windows explorer definition since this is used cross-platform.
        /// </summary>
        public static decimal ConvertBytesToMegabytes(long numOfBytes)
        {
            return Convert.ToDecimal(numOfBytes) / (1000.0m * 1000.0m);
        }
    }
}
