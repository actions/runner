using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.Services.Common
{
    public static class StreamUtility
    {
        /// <summary>
        /// Copies the contents of the reader to the output.
        /// Column data (and header names if included) are separated by delimiter.
        /// </summary>
        /// <param name="reader">The Reader to read data from</param>
        /// <param name="output">where to write the output to</param>
        /// <param name="delimiter">used to separate row data (and the column headers)</param>
        /// <param name="includeHeader">by default headers are included, setting this to false will exclude them</param>
        /// <param name="headerPrefix">optional string to prefix the headers with</param>
        /// <param name="rowPrefix">optional string to prefix each row with</param>
        /// <param name="max">when null all data from reader is copied, when non-null only that many rows are copied</param>
        static public void Copy(IDataReader reader, TextWriter output, string delimiter = ",", bool includeHeader = true, string headerPrefix = null, string rowPrefix = null, int? max = null)
        {
            ArgumentUtility.CheckForNull(reader, nameof(reader));
            ArgumentUtility.CheckForNull(output, nameof(output));

            bool hasPrefix = !string.IsNullOrEmpty(headerPrefix) || !string.IsNullOrEmpty(rowPrefix);
            
            List<string> row = new List<string>();
            if (includeHeader)
            {
                if (hasPrefix)
                {
                    row.Add(headerPrefix);
                }
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.GetName(i));
                }
                output.WriteLine(string.Join(delimiter, row));
            }

            int count = 0;
            while (reader.Read())
            {
                count++;

                if (max.HasValue && count > max.Value)
                {
                    output.WriteLine($"There were more than {max} results, the rest have been omitted");
                    break;
                }

                row.Clear();
                if (hasPrefix)
                {
                    row.Add(rowPrefix);
                }

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    object value = reader[i];
                    string stringValue;
                    if (value == null || value is DBNull)
                    {
                        stringValue = "<null>";
                    }
                    else if (value is byte[])
                    {
                        byte[] array = (byte[])value;
                        StringBuilder stringBuilder = new StringBuilder("0x", array.Length * 2 + 2);

                        for (int index = 0; index < array.Length; ++index)
                        {
                            stringBuilder.Append(array[index].ToString("X2", CultureInfo.InvariantCulture));
                        }

                        stringValue = stringBuilder.ToString();
                    }
                    else
                    {
                        stringValue = value.ToString();
                    }
                    row.Add(stringValue);
                }
                output.WriteLine(string.Join(delimiter, row));
            }
        }
    }
}
