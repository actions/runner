using System;
using System.ComponentModel;
using System.Text;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TimelineRecordIdGenerator
    {
        public static Guid GetId(String refName)
        {
            byte[] bytes = Encoding.BigEndianUnicode.GetBytes(refName);
            var sha1ForNonSecretPurposes = new Sha1ForNonSecretPurposes();
            sha1ForNonSecretPurposes.Start();
            sha1ForNonSecretPurposes.Append(namespaceBytes);
            sha1ForNonSecretPurposes.Append(bytes);
            Array.Resize<byte>(ref bytes, 16);
            sha1ForNonSecretPurposes.Finish(bytes);
            bytes[7] = (byte)((bytes[7] & 15) | 80);
            return new Guid(bytes);
        }

        // Value of 'DistributedTask.Pipelines' encoded without the namespace bytes on the front
        private static readonly byte[] namespaceBytes = new byte[]
        {
                83,
                55,
                27,
                127,
                212,
                97,
                75,
                93,
                197,
                226,
                39,
                51,
                83,
                35,
                223,
                36
        };

        private struct Sha1ForNonSecretPurposes
        {
            private long length;

            private uint[] w;

            private int pos;

            public void Start()
            {
                if (this.w == null)
                {
                    this.w = new uint[85];
                }
                this.length = 0L;
                this.pos = 0;
                this.w[80] = 1732584193u;
                this.w[81] = 4023233417u;
                this.w[82] = 2562383102u;
                this.w[83] = 271733878u;
                this.w[84] = 3285377520u;
            }

            public void Append(byte input)
            {
                this.w[this.pos / 4] = (this.w[this.pos / 4] << 8 | (uint)input);
                int arg_35_0 = 64;
                int num = this.pos + 1;
                this.pos = num;
                if (arg_35_0 == num)
                {
                    this.Drain();
                }
            }

            public void Append(byte[] input)
            {
                for (int i = 0; i < input.Length; i++)
                {
                    byte input2 = input[i];
                    this.Append(input2);
                }
            }

            public void Finish(byte[] output)
            {
                long num = this.length + (long)(8 * this.pos);
                this.Append(128);
                while (this.pos != 56)
                {
                    this.Append(0);
                }
                this.Append((byte)(num >> 56));
                this.Append((byte)(num >> 48));
                this.Append((byte)(num >> 40));
                this.Append((byte)(num >> 32));
                this.Append((byte)(num >> 24));
                this.Append((byte)(num >> 16));
                this.Append((byte)(num >> 8));
                this.Append((byte)num);
                int num2 = (output.Length < 20) ? output.Length : 20;
                for (int num3 = 0; num3 != num2; num3++)
                {
                    uint num4 = this.w[80 + num3 / 4];
                    output[num3] = (byte)(num4 >> 24);
                    this.w[80 + num3 / 4] = num4 << 8;
                }
            }

            private void Drain()
            {
                for (int num = 16; num != 80; num++)
                {
                    this.w[num] = Rol1(this.w[num - 3] ^ this.w[num - 8] ^ this.w[num - 14] ^ this.w[num - 16]);
                }
                uint num2 = this.w[80];
                uint num3 = this.w[81];
                uint num4 = this.w[82];
                uint num5 = this.w[83];
                uint num6 = this.w[84];
                for (int num7 = 0; num7 != 20; num7++)
                {
                    uint num8 = (num3 & num4) | (~num3 & num5);
                    uint num9 = Rol5(num2) + num8 + num6 + 1518500249u + this.w[num7];
                    num6 = num5;
                    num5 = num4;
                    num4 = Rol30(num3);
                    num3 = num2;
                    num2 = num9;
                }
                for (int num10 = 20; num10 != 40; num10++)
                {
                    uint num11 = num3 ^ num4 ^ num5;
                    uint num12 = Rol5(num2) + num11 + num6 + 1859775393u + this.w[num10];
                    num6 = num5;
                    num5 = num4;
                    num4 = Rol30(num3);
                    num3 = num2;
                    num2 = num12;
                }
                for (int num13 = 40; num13 != 60; num13++)
                {
                    uint num14 = (num3 & num4) | (num3 & num5) | (num4 & num5);
                    uint num15 = Rol5(num2) + num14 + num6 + 2400959708u + this.w[num13];
                    num6 = num5;
                    num5 = num4;
                    num4 = Rol30(num3);
                    num3 = num2;
                    num2 = num15;
                }
                for (int num16 = 60; num16 != 80; num16++)
                {
                    uint num17 = num3 ^ num4 ^ num5;
                    uint num18 = Rol5(num2) + num17 + num6 + 3395469782u + this.w[num16];
                    num6 = num5;
                    num5 = num4;
                    num4 = Rol30(num3);
                    num3 = num2;
                    num2 = num18;
                }
                this.w[80] += num2;
                this.w[81] += num3;
                this.w[82] += num4;
                this.w[83] += num5;
                this.w[84] += num6;
                this.length += 512L;
                this.pos = 0;
            }

            private static uint Rol1(uint input)
            {
                return input << 1 | input >> 31;
            }

            private static uint Rol5(uint input)
            {
                return input << 5 | input >> 27;
            }

            private static uint Rol30(uint input)
            {
                return input << 30 | input >> 2;
            }
        }
    }
}
