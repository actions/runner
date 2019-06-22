using System;
using System.Threading;

namespace GitHub.Services.BlobStore.Common
{
    public interface ILongHash
    {
        long GetLongHashCode();
    }

    public enum BloomFilterCheckResult
    {
        MaybeInserted,
        DefinitelyNotInserted
    }

    [CLSCompliant(false)]
    public class BloomFilter<T> where T : ILongHash
    {
        private readonly long[] elements;
        public readonly ulong BitCount;
        private const int bitsPerElement = 64;

        private long bitsSet = 0;
        public long BitsSet => Interlocked.Read(ref bitsSet);

        private static ulong DivideRoundUp(ulong dividend, ulong divisor)
        {
            return (dividend + divisor - 1) / divisor;
        }

        public BloomFilter(ulong size)
        {
            BitCount = size;
            ulong elementCount = DivideRoundUp(size, bitsPerElement);
            elements = new long[elementCount];
        }

        public void Insert(T id)
        {
            if (SetBit(id))
            {
                Interlocked.Increment(ref bitsSet);
            }
        }

        public BloomFilterCheckResult Check(T id)
        {
            return CheckBit(id)
                ? BloomFilterCheckResult.MaybeInserted
                : BloomFilterCheckResult.DefinitelyNotInserted;
        }

        private void GetBitIndices(T id, out ulong elementIndex, out long bitMask)
        {
            long longHash = id.GetLongHashCode();
            ulong hash;
            unchecked
            {
                hash = (ulong)longHash;
            }
            ulong bitIndex = hash % BitCount;
            elementIndex = bitIndex / bitsPerElement;
            bitMask = 1L << (int)(long)(bitIndex %= bitsPerElement);
        }

        private bool CheckBit(T id)
        {
            ulong elementIndex;
            long bitMask;
            GetBitIndices(id, out elementIndex, out bitMask);
            long currentValue = Interlocked.Read(ref elements[elementIndex]);
            return ((currentValue & bitMask) != 0);
        }

        private bool SetBit(T id)
        {
            ulong elementIndex;
            long bitMask;
            GetBitIndices(id, out elementIndex, out bitMask);

            long currentValue;
            long updatedValue;
            do
            {
                currentValue = Interlocked.Read(ref elements[elementIndex]);
                if ((currentValue & bitMask) != 0)
                {
                    return false;
                }
                updatedValue = currentValue | bitMask;
            } while (currentValue != Interlocked.CompareExchange(ref elements[elementIndex], updatedValue, currentValue));

            return true;
        }
    }
}
