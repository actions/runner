//*************************************************************************************************
// ArrayUtil.cs
//
// A class with random array processing helper routines.
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//*************************************************************************************************
using System;
using System.Diagnostics;
using System.Text;

namespace GitHub.Services.Common
{
    //********************************************************************************************
    /// <summary>
    /// A class with random array processing helper routines.
    /// </summary>
    //********************************************************************************************
    public static class ArrayUtility
    {
        //****************************************************************************************
        /// <summary>
        /// Compare two byte arrays to determine if they contain the same data.
        /// </summary>
        /// <param name="a1">First array to compare.</param>
        /// <param name="a2">Second array to compare.</param>
        /// <returns>true if the arrays are equal and false if not.</returns>
        //****************************************************************************************
        public unsafe static bool Equals(byte[] a1, byte[] a2)
        {
            Debug.Assert(a1 != null, "a1 was null");
            Debug.Assert(a2 != null, "a2 was null");
            
            // Check if the lengths are the same.
            if (a1.Length != a2.Length)
            {
                return false;
            }
            if (a1.Length == 0)
            {
                return true;
            }

            return Equals(a1, a2, a1.Length);
        }

        //****************************************************************************************
        /// <summary>
        /// Generate hash code for a byte array.
        /// </summary>
        /// <param name="array">array to generate hash code for.</param>
        /// <returns>hash generated from the array members.</returns>
        //****************************************************************************************
        public static int GetHashCode(byte[] array)
        {
            Debug.Assert(array != null, "array was null");

            int hash = 0;
            // the C# compiler defaults to unchecked behavior, so this will
            // wrap silently.  Since this is a hash code and not a count, this
            // is fine with us.
            foreach (byte item in array)
            {
                hash += item;
            }

            return hash;
        }

        //****************************************************************************************
        /// <summary>
        /// Compare two byte arrays to determine if they contain the same data.
        /// </summary>
        /// <param name="a1">First array to compare.</param>
        /// <param name="a2">Second array to compare.</param>
        /// <param name="length"># of bytes to compare.</param>
        /// <returns>true if the arrays are equal and false if not.</returns>
        //****************************************************************************************
        public unsafe static bool Equals(byte[] a1, byte[] a2, int length)
        {
            // Pin the arrays so that we can use unsafe pointers to compare an int at a time.
            fixed (byte* p1 = &a1[0])
            {
                fixed (byte* p2 = &a2[0])
                {
                    // Get temps for the pointers because you can't change fixed pointers.
                    byte* q1 = p1, q2 = p2;

                    // Compare an int at a time for as long as we can.  We divide by four because an int
                    // is always 32 bits in C# regardless of platform.
                    int i;
                    for (i = length >> 2; i > 0; --i)
                    {
                        if (*((int*) q1) != *((int*) q2))
                        {
                            return false;
                        }
                        q1 += sizeof(int);
                        q2 += sizeof(int);
                    }

                    // Compare a byte at a time for the remaining bytes (0 - 3 of them).  This also
                    // depends on ints being 32 bits.
                    for (i = length & 0x3; i > 0; --i)
                    {
                        if (*q1 != *q2)
                        {
                            return false;
                        }
                        ++q1;
                        ++q2;
                    }
                }
            }
            return true;
        }
        
        //****************************************************************************************
        /// <summary>
        /// Convert the byte array to a lower case hex string.
        /// </summary>
        /// <param name="bytes">byte array to be converted.</param>
        /// <returns>hex string converted from byte array.</returns>
        //****************************************************************************************
        public static String StringFromByteArray(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return "null";
            }

            StringBuilder sb = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];

                char first = (char)(((b >> 4) & 0x0F) + 0x30);
                char second = (char)((b & 0x0F) + 0x30);

                sb.Append(first >= 0x3A ? (char)(first + 0x27) : first);
                sb.Append(second >= 0x3A ? (char)(second + 0x27) : second);
            }

            return sb.ToString();
        }
    }
} // namespace
