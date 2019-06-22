using NTSTATUS = System.UInt32;
using ULONG = System.UInt32;
using LONG = System.Int32;
using ULONG_PTR = System.UInt64; // TODO UGH
using LONG_PTR = System.Int64; // TODO UGH
using UCHAR = System.Byte;
using USHORT = System.UInt16;
using INT = System.Int32;
using System;
using System.Diagnostics;

namespace GitHub.Services.BlobStore.Common
{
    // https://microsoft.visualstudio.com/OS/_git/os?path=%2Fminkernel%2Fntos%2Frtl%2Fxpress.c&version=GBofficial%2Frsmaster&_a=contents
    [CLSCompliant(false)]
    public static class XpressManaged
    {
        private static readonly Pool<XpressManaged.XPRESS_LZ_WORKSPACE_MAX> ManagedWorkspacePool = new Pool<XpressManaged.XPRESS_LZ_WORKSPACE_MAX>(
            () => new XpressManaged.XPRESS_LZ_WORKSPACE_MAX(),
            w => { },
            maxToKeep: 4 * Environment.ProcessorCount);

        private static void NT_ASSERT(bool assert)
        {
            if (!assert)
            {
                throw new InvalidOperationException();
            }
        }

        private static unsafe void __movsb(UCHAR* dst, UCHAR* source, ULONG_PTR count)
        {
            for (ULONG_PTR i = 0; i < count; i++)
            {
                dst[i] = source[i];
            }
        }

        private const NTSTATUS STATUS_BUFFER_TOO_SMALL = 0xC0000023;
        private const NTSTATUS STATUS_NOT_SUPPORTED = 0xC00000BB;
        private const NTSTATUS STATUS_SUCCESS = 0;
        private const NTSTATUS STATUS_BAD_COMPRESSION_BUFFER = 0xC0000242;

        //
        // LZ77 defines and structs.
        //

        private const uint FIRST_HASH_COEFF = 4;
        private const uint FIRST_HASH_COEFF2 = 1;

        private const uint HASH_TABLE_SIZE = 255 * FIRST_HASH_COEFF * 2 + 255 * FIRST_HASH_COEFF2 * 2 + 255 + 1;

        private const uint HASH_COEFF_HIGH = 8;
        private const uint HASH_COEFF_LOW = 2;

        private const uint HASH_TABLE_EX_SIZE = (255 + 255) * HASH_COEFF_HIGH + 255 * HASH_COEFF_LOW + 255 + 1;

        private const uint LZ_WINDOW_SIZE = 1 << 13;
        private const uint LZ_SMALL_LEN_BITS = 3;
        private const uint LZ_MAX_SMALL_LEN = ((1 << (int)LZ_SMALL_LEN_BITS) - 1);

        private const uint Z_HASH_SIZE = (1 << 15);

        private unsafe sealed class XPRESS_LZ_WORKSPACE_MAX
        {
            public readonly UCHAR*[] HashTable = new UCHAR*[Z_HASH_SIZE];
            public readonly UCHAR*[] PrevMatch = new UCHAR*[LZ_WINDOW_SIZE * 2];

            public void Zero()
            {
                for (int i = 0; i < HashTable.Length; i++)
                {
                    HashTable[i] = null;
                }

                for (int i = 0; i < PrevMatch.Length; i++)
                {
                    PrevMatch[i] = null;
                }
            }
        }

        public static uint TryCompressChunk(
            byte[] uncompressedChunk,
            uint uncompressedChunkSize,
            byte[] compressedChunk,
            out uint compressedChunkSize)
        {
            if (uncompressedChunkSize < 16)
            {
                compressedChunkSize = 0;
                return STATUS_BUFFER_TOO_SMALL;
            }

            using (var workspace = ManagedWorkspacePool.Get())
            {
                return RtlCompressBufferXpressLzMax(
                    uncompressedChunk,
                    (uint)uncompressedChunkSize,
                    compressedChunk,
                    (uint)compressedChunk.Length,
                    out compressedChunkSize,
                    workspace.Value);
            }
        }

        /*++

XpressHashFunction - This table was originally created by the following code:

VOID
XpressInitializeHashFunction (
    VOID
    )
{
    ULONG RandVal1;
    ULONG RandVal2;
    INT RandVal3;
    INT RandVal4;
    INT Diff;
    ULONG_PTR i;
    ULONG_PTR j;
    ULONG_PTR k;

    RandVal1 = 0x13579bdfL;
    RandVal2 = 0x87654321L;
    RandVal3 = 0x9e3779b9L;

    for (k = 0; k < 3; ++k) {
        for(j = 0; j < 256; ++j) {
            RandVal3 = RandVal2;
            RandVal4 = RandVal1;
            Diff = 0;
            for (i = 0; i < 32; ++i) {
                Diff += 0x9e3779b9;
                RandVal3 += Diff;
                RandVal4 += Diff;
                RandVal1 += ((RandVal2<<3) + RandVal3) ^ (RandVal2 + Diff) ^
                            ((RandVal2>>5) + RandVal4);
                RandVal1 += ((RandVal1<<3) + RandVal4) ^ (RandVal1 + Diff) ^
                            ((RandVal1>>5) + RandVal3);
            }
            RandVal1 += RandVal2;
            XpressHashFunction[k][j] = (USHORT)(RandVal1 % Z_HASH_SIZE);
        }
    }

    XpressInitializedHashFunction = 1;

    return;
}

--*/

        private static readonly USHORT[][] XpressHashFunction = new USHORT[][] {
            new USHORT[] {5732,  14471,  24297,  25128,  11502,  22712,  22856,  21969,  16029,  23951,  1785,  13794,  3705,  1145,  16537,  7129,  26156,  31870,  23418,  20567,  29626,  218,  7372,  1940,  17184,  18323,  10119,  8604,  23621,  13384,  29712,  9146,  20553,  27100,  5850,  23503,  23176,  28142,  4676,  27280,  31756,  8706,  15559,  27626,  18819,  3281,  10692,  4048,  20259,  10493,  27969,  7260,  24815,  17549,  18994,  16693,  31156,  16887,  8459,  31718,  32718,  12366,  7181,  15493,  24345,  26476,  7355,  14175,  13292,  4822,  22793,  215,  24723,  706,  21435,  19075,  25807,  32292,  7815,  29338,  10789,  9301,  21695,  25084,  25689,  31272,  17247,  14211,  9189,  26756,  18524,  14732,  6188,  8560,  13667,  26189,  32660,  4089,  3976,  23040,  29476,  20495,  7315,  5110,  28586,  17798,  29904,  344,  22387,  27111,  9782,  6030,  18509,  7423,  9197,  31788,  25654,  20364,  18354,  5971,  10938,  366,  15563,  18371,  10683,  3657,  18511,  15438,  5520,  422,  13442,  29830,  27837,  15463,  14806,  12921,  3883,  22276,  16387,  31729,  21655,  19236,  20470,  20492,  11751,  24686,  25844,  2840,  9189,  9869,  19238,  24848,  15480,  25809,  20681,  11470,  25986,  9409,  24755,  16524,  24107,  21258,  13853,  28403,  6006,  21415,  23280,  2570,  12055,  20113,  5235,  3562,  7055,  733,  30365,  9470,  24356,  9178,  21362,  28496,  11136,  3731,  3047,  16021,  11840,  22838,  907,  18609,  4721,  18943,  14439,  26839,  2422,  15312,  11641,  25979,  199,  31769,  5234,  23588,  23401,  15374,  1558,  14750,  24149,  31127,  13862,  26020,  31010,  1888,  11434,  1688,  5562,  9959,  2280,  8742,  1443,  25709,  10904,  24657,  23884,  6380,  4008,  20069,  28033,  6303,  19272,  30233,  17279,  17817,  24129,  28759,  935,  6698,  16287,  32578,  14420,  17587,  18645,  17233,  8481,  15766,  3346,  4080,  25415,  11667,  15445,  3149,  17290,  14358,  28128,  29424,  17465,  20225,  8897,  19481},
            new USHORT[] {89,  24313,  14591,  8306,  22828,  18884,  7990,  26457,  24877,  30705,  24165,  7973,  16319,  9286,  22677,  30021,  7901,  7880,  18035,  4008,  2401,  29585,  21569,  12793,  23315,  9286,  10852,  26539,  13186,  26156,  26964,  21604,  8305,  15916,  17767,  15170,  26565,  26259,  11693,  31316,  28980,  22665,  496,  16463,  12261,  5111,  1943,  1660,  23930,  13257,  4757,  31563,  15469,  18763,  17906,  21517,  29723,  18324,  20062,  23554,  9039,  24669,  31775,  16771,  17119,  31060,  2587,  8261,  22033,  17027,  5593,  1729,  214,  2914,  21849,  18432,  7429,  21490,  26117,  29863,  22371,  18408,  792,  19364,  21668,  1809,  21594,  4948,  20456,  2019,  29290,  22887,  29078,  22335,  9187,  5096,  17146,  1432,  19366,  22510,  7422,  25502,  13009,  11542,  7875,  2647,  7784,  12205,  12243,  19397,  15740,  5364,  4113,  26061,  25497,  7856,  1925,  32327,  12210,  6254,  27122,  27016,  20121,  12018,  4980,  9272,  5017,  10708,  31072,  25162,  25851,  13203,  15909,  30345,  26602,  19838,  12429,  4695,  2383,  17491,  524,  31579,  1318,  12371,  19282,  19629,  1772,  22021,  12775,  25516,  2257,  26301,  10519,  27741,  5552,  27266,  13548,  28363,  18524,  31245,  5982,  17294,  21585,  13591,  31302,  31804,  12702,  15533,  29640,  23889,  24312,  16503,  15054,  22097,  17733,  4557,  22693,  3477,  16700,  18113,  11692,  10926,  2215,  9617,  24248,  3956,  22701,  24952,  938,  13889,  4191,  24275,  9101,  15744,  19304,  12082,  6459,  17626,  32298,  2736,  8529,  28611,  15671,  3892,  26773,  25900,  6541,  24135,  20603,  24870,  27926,  4019,  28502,  28252,  10220,  5251,  25639,  26053,  25351,  9722,  3020,  4086,  29133,  25585,  23781,  19564,  29020,  23744,  1752,  30531,  24484,  30451,  25913,  10908,  15852,  19700,  14122,  26590,  17988,  5299,  23511,  22145,  26960,  9847,  5119,  18466,  6431,  3592,  6992,  7398,  9792,  24368,  19780,  27824,  16766,  770},
            new USHORT[] {29141,  2944,  21483,  667,  28990,  23448,  12644,  7839,  21929,  19747,  16616,  17046,  19188,  32762,  25138,  25039,  19337,  724,  29934,  4914,  22687,  841,  14193,  22961,  1775,  6902,  23188,  19240,  7069,  25600,  15642,  4994,  21651,  3594,  27731,  19933,  11672,  20837,  21867,  2547,  30691,  5021,  4084,  3381,  20986,  2656,  7110,  13821,  7795,  758,  20780,  20822,  32649,  9811,  2267,  25889,  11350,  27423,  2944,  7104,  22471,  31485,  31150,  9359,  30674,  13639,  31985,  20817,  11744,  16516,  11270,  24524,  3193,  18291,  5290,  7973,  25154,  32008,  17754,  3315,  27005,  21741,  15695,  20415,  8565,  4083,  23560,  24858,  24228,  13255,  14780,  14373,  22361,  20804,  2970,  16847,  8003,  25347,  6633,  29140,  25152,  16751,  10005,  8413,  31873,  12712,  28180,  23299,  16433,  3658,  7784,  28886,  19894,  18771,  675,  588,  901,  24092,  1755,  30519,  11912,  15045,  15684,  9183,  10056,  16848,  16248,  32429,  2555,  11360,  11926,  32162,  19499,  10997,  20341,  5905,  16620,  32124,  27807,  19460,  24198,  905,  4976,  14495,  17752,  15076,  31994,  11620,  27478,  16025,  31463,  25965,  28887,  18086,  3806,  11346,  6701,  27480,  30042,  61,  1846,  16527,  9096,  5811,  3284,  1002,  21170,  16860,  21152,  4570,  10196,  32752,  9201,  22647,  16755,  32259,  29729,  23205,  19906,  20825,  31181,  3237,  931,  25156,  20188,  16427,  14394,  18993,  7857,  25179,  26064,  1679,  23786,  32761,  10299,  1891,  14039,  1035,  19354,  6436,  15366,  14679,  26868,  19947,  4862,  19105,  7407,  13039,  4013,  22970,  16180,  14412,  3405,  4984,  26696,  7035,  5361,  11923,  20784,  23477,  9498,  8836,  25922,  32629,  27125,  30994,  18141,  21981,  27383,  23834,  24366,  10855,  6149,  22048,  11990,  13549,  4315,  3591,  1901,  21868,  23189,  25251,  28174,  6620,  11566,  31561,  5909,  10506,  5137,  8212,  20000,  14345,  17393,  7349,  17202,  15562}};

        private static readonly UCHAR[] XpressHighBitIndexTable = new UCHAR[] { 0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7 };

        private static unsafe NTSTATUS RtlCompressBufferXpressLzMax(
            byte[] uncompressedBuffer,
            uint uncompressedBufferSize,
            byte[] compressedBuffer,
            uint compressedBufferSize,
            out uint finalCompressedSize,
            XPRESS_LZ_WORKSPACE_MAX Workspace)
        {
            Workspace.Zero();

            fixed (UCHAR* ub = uncompressedBuffer)
            fixed (UCHAR* cb = compressedBuffer)
            fixed (ULONG* outSize = &finalCompressedSize)
            fixed (UCHAR** WorkspaceHashTable = Workspace.HashTable)
            fixed (UCHAR** WorkspacePrevMatch = Workspace.PrevMatch)
            {
                return RtlCompressBufferXpressLzMaxInternal(
                    ub,
                    uncompressedBufferSize,
                    cb,
                    compressedBufferSize,
                    outSize,
                    WorkspaceHashTable,
                    WorkspacePrevMatch);
            }
        }

        private static unsafe NTSTATUS
RtlCompressBufferXpressLzMaxInternal(
   /* IN */ UCHAR* UncompressedBuffer,
   /* IN */ ULONG UncompressedBufferSize,
   /* OUT */ UCHAR* CompressedBuffer,
   /* IN */ ULONG CompressedBufferSize,
   /* OUT */ ULONG* FinalCompressedSize,
    // /* IN */ XPRESS_LZ_WORKSPACE_MAX WorkspaceClass
    UCHAR** WorkspaceHashTable,
    UCHAR** WorkspacePrevMatch
    //  /* IN */ PRTL_XPRESS_CALLBACK_FUNCTION Callback,
    //  /* IN */ PVOID CallbackContext,
    // /* IN */ ULONG ProgressBytes
    )

        /*++

        Routine Description:

            This routine takes as input an uncompressed buffer and produces its
            compressed equivalent provided the compressed data fits within the specified
            destination buffer.

            An output variable indicates the number of bytes used to store the
            compressed buffer.

            This function uses an LZ77 method that emphasizes size over speed.

        Arguments:

            UncompressedBuffer - Supplies a pointer to the uncompressed data.

            UncompressedBufferSize - Supplies the size, in bytes, of the uncompressed
                buffer.

            CompressedBuffer - Supplies a pointer to where the compressed data is to be
                stored.

            CompressedBufferSize - Supplies the size, in bytes, of the compressed
                buffer.

            FinalCompressedSize - Receives the number of bytes needed in the compressed
                buffer to store the compressed data.

            WorkSpace - A buffer for compression data structures.

            Callback - The function to call back periodically.

            CallbackContext - A PVOID that will be passed to the Callback function.

            ProgressBytes - The callback will be invoked each time this many bytes are
                compressed.  (Note that this is the uncompressed size, not the
                compressed size).  So, if the caller wants the callback to be called
                about 7 times, it should specify ProgressBytes as
                UncompressedBufferSize/8.

        Return Value:

            STATUS_SUCCESS - Compression was successful.

            STATUS_BUFFER_TOO_SMALL - The compressed buffer is too small to hold the
                compressed data.

        --*/

        {
            UCHAR* SafeBufferEnd;
            UCHAR* SafeOutputBufferEnd;
            UCHAR* InputBufferEnd;
            UCHAR* OutputBufferEnd;
            UCHAR* InputPos;
            UCHAR* OutputPos;
            UCHAR* Match;
            UCHAR* SavedInputPos;
            UCHAR* LastLenHalfByte;
            UCHAR* WindowBound;
            UCHAR* ProgressMark;
            ULONG RunningTags;
            ULONG /* UNALIGNED */ * TagsOut;
            ULONG /* UNALIGNED */ * InputPosLong;
            ULONG /* UNALIGNED */ * MatchLong;
            ULONG_PTR i;
            ULONG_PTR HashValue;
            ULONG_PTR MatchLen;
            ULONG_PTR MatchOffset = 0;
            ULONG_PTR BestMatchLen;
            ULONG_PTR HashWindowIndex;
            ULONG Next4;
            ULONG Xor4;
            ULONG ProgressBytes = UncompressedBufferSize;
            UCHAR* HashPos;
            UCHAR* HashBound;
            UCHAR** PrevMatchList;
            // XPRESS_CALLBACK_PARAMS CallbackParams;

            bool gotoExtendMatch = false;
            bool gotoEncodeExtraLen = false;

            //
            // Many aspects of this function are similar to the
            // RtlCompressBufferXpressLzStandard function, so those comments will not be
            // repeated here.
            //

            InputBufferEnd = UncompressedBuffer + UncompressedBufferSize;
            OutputBufferEnd = CompressedBuffer + CompressedBufferSize;

            if (CompressedBufferSize < 64 || UncompressedBufferSize < 8)
            {
                return STATUS_BUFFER_TOO_SMALL;
            }

            if (UncompressedBuffer <= (UCHAR*)(LZ_WINDOW_SIZE + 1))
            {

                //
                // We don't support pointers so close to NULL.
                //

                return STATUS_NOT_SUPPORTED;
            }

            //
            // Initialize the hash table.
            // NULL is always outside the window, so it will terminate the match search.
            //

            // RtlZeroMemory(&Workspace.HashTable, sizeof(Workspace.HashTable));
            // Handled by XPRESS_LZ_WORKSPACE_MAX.Zero()

            //
            // Set up "safe" input and output bounds so that we don't have to do as many
            // bounds checks.  The input bound is set up to let us look at 4 bytes for
            // match comparisons.  The output bound lets us write out 32 literals and
            // one match.
            //

            SafeBufferEnd = InputBufferEnd - 5;
            SafeOutputBufferEnd = OutputBufferEnd - 32 - 9;

            InputPos = UncompressedBuffer;
            OutputPos = CompressedBuffer;
            LastLenHalfByte = null;

            //if (Callback == NULL ||
            //    ProgressBytes > UncompressedBufferSize)
            //{
            //    ProgressBytes = UncompressedBufferSize;
            //}

            //CallbackParams.Callback = Callback;
            //CallbackParams.CallbackContext = CallbackContext;
            //CallbackParams.ProgressBytes = ProgressBytes;

            RunningTags = 2;
            TagsOut = (ULONG /* UNALIGNED */ *)OutputPos;
            OutputPos += sizeof(ULONG);

            OutputPos[0] = InputPos[0];
            ++OutputPos;
            ++InputPos;

            BestMatchLen = 3;
            i = 0;

            HashWindowIndex = 0;
            HashPos = UncompressedBuffer;

            for (; ; )
            {

                //
                // We use a pre-hashing phase to reduce cache misses.  This way, we
                // don't have to touch the hash table or the hash function while
                // searching for matches.  Hash one window size in advance.
                //

                HashBound = HashPos + LZ_WINDOW_SIZE;

                if (HashBound > SafeBufferEnd)
                {
                    HashBound = SafeBufferEnd;
                }

                ProgressMark = (byte*)Math.Min((ULONG_PTR)HashBound, (ULONG_PTR)InputPos + ProgressBytes);

                HashWindowIndex %= LZ_WINDOW_SIZE * 2;

                while (HashPos < HashBound)
                {

                    HashValue = XpressHashFunction[0][HashPos[0]];
                    HashValue ^= XpressHashFunction[1][HashPos[1]];
                    HashValue ^= XpressHashFunction[2][HashPos[2]];

                    //
                    // Find the nearest three-byte match and save it.
                    //

                    NT_ASSERT(HashWindowIndex < LZ_WINDOW_SIZE * 2);

                    WorkspacePrevMatch[HashWindowIndex] =
                        WorkspaceHashTable[HashValue];

                    WorkspaceHashTable[HashValue] = HashPos;

                    ++HashPos;
                    ++HashWindowIndex;
                }

                PrevMatchList = &WorkspacePrevMatch[0];

                for (; ; )
                {

                    for (; ; )
                    {

                        //
                        // If we finished the part of the buffer that we've hashed, we
                        // need to hash the next part before we can continue.
                        //

                        if (InputPos >= ProgressMark)
                        {

                            if (InputPos >= HashBound)
                            {
                                goto HashWindowDone;
                            }

                            //ProgressMark = RtlpMakeXpressCallback(&CallbackParams,
                            //                                      HashBound,
                            //                                      InputPos);
                        }

                        //
                        // The PrevMatch structure acts like a collection of lists.  We
                        // will traverse the list that starts from our current position.
                        //

                        Match = PrevMatchList[(ULONG_PTR)(InputPos - UncompressedBuffer)
                                              % (LZ_WINDOW_SIZE * 2)];

                        Next4 = *((ULONG /* UNALIGNED */ *)InputPos);

                        //
                        // First, make sure this potential match is in the window.
                        //

                        if (Match >= InputPos - LZ_WINDOW_SIZE)
                        {

                            WindowBound = InputPos - LZ_WINDOW_SIZE;

                            //
                            // Xor four bytes with the match.  If the result is zero, we
                            // have a four byte match (which may be longer).  If not,
                            // then AND the xor with 0xffffff, and if that's zero, we
                            // have a three-byte match (which is not any longer).
                            //

                            Xor4 = Next4 ^ *((ULONG /* UNALIGNED */ *)Match);

                            if (Xor4 == 0)
                            {
                                gotoExtendMatch = true;
                                goto ExtendMatch1;
                            }
                            else if ((Xor4 & 0xffffff) == 0)
                            {
                                break;
                            }

                            //
                            // No match yet.  This can happen if we have hash
                            // collisions.  Try the next match in the list.
                            //

                            Match = PrevMatchList[(ULONG_PTR)(Match - UncompressedBuffer) %
                                                  (LZ_WINDOW_SIZE * 2)];

                            if (Match >= WindowBound)
                            {

                                Xor4 = Next4 ^ *((ULONG /* UNALIGNED */ *)Match);

                                if (Xor4 == 0)
                                {
                                    gotoExtendMatch = true;
                                    goto ExtendMatch1;
                                }
                                else if ((Xor4 & 0xffffff) == 0)
                                {
                                    break;
                                }

                                //
                                // Try one more before giving up.
                                //

                                Match = PrevMatchList[(ULONG_PTR)(Match - UncompressedBuffer)
                                                      % (LZ_WINDOW_SIZE * 2)];

                                if (Match >= WindowBound)
                                {

                                    Xor4 = Next4 ^ *((ULONG /* UNALIGNED */ *)Match);

                                    if (Xor4 == 0)
                                    {
                                        gotoExtendMatch = true;
                                        goto ExtendMatch1;
                                    }
                                    else if ((Xor4 & 0xffffff) == 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        //
                        // We didn't find a match in the window.  Output a literal.
                        //

                        OutputPos[0] = (UCHAR)Next4; //InputPos[0];
                        ++OutputPos;
                        ++InputPos;

                        if ((INT)RunningTags > 0)
                        {
                            RunningTags *= 2;
                            continue;
                        }

                        RunningTags *= 2;
                        *TagsOut = RunningTags;
                        RunningTags = 1;
                        TagsOut = (ULONG /* UNALIGNED */ *)OutputPos;
                        OutputPos += sizeof(ULONG);

                        if (OutputPos >= SafeOutputBufferEnd)
                        {
                            goto SafeDone;
                        }
                    }

                    //
                    // If we're here, then Match points to an exactly-three-byte match.
                    // Save this as the best so far, then start looking for something
                    // longer.
                    //

                    NT_ASSERT(BestMatchLen == 3);
                    NT_ASSERT(InputPos > Match);
                    MatchOffset = (ULONG_PTR)(InputPos - Match);
                    WindowBound = InputPos - LZ_WINDOW_SIZE;

                    ExtendMatch1:

                    for (; ; )
                    {
                        if (gotoExtendMatch)
                        {
                            goto ExtendMatch2;
                        }

                        Match = PrevMatchList[(ULONG_PTR)(Match - UncompressedBuffer) %
                                              (LZ_WINDOW_SIZE * 2)];

                        if (Match < WindowBound)
                        {

                            //
                            // Since we're walking the matches in the order they occur
                            // in the buffer, as soon as one is outside the window, we
                            // can stop searching.
                            //

                            break;
                        }

                        //
                        // If we're here, we must have already found a match that is at
                        // least three bytes.  So just check directly for a four-byte
                        // match.
                        //

                        ExtendMatch2:

                        if (gotoExtendMatch || Next4 == *((ULONG /* UNALIGNED */ *)Match))
                        {

                            // ExtendMatch:
                            gotoExtendMatch = false;

                            SavedInputPos = InputPos;

                            //
                            // At this point, the current match is at least four bytes.
                            // See how long it extends.
                            //

                            InputPos += 4;
                            Match += 4;

                            InputPosLong = (ULONG /* UNALIGNED */ *)InputPos;
                            MatchLong = (ULONG /* UNALIGNED */ *)Match;

                            while (InputPos + 32 < InputBufferEnd)
                            {
                                if (InputPosLong[0] != MatchLong[0])
                                { goto FinishMatch; }
                                if (InputPosLong[1] != MatchLong[1])
                                { InputPos += 4; Match += 4; goto FinishMatch; }
                                if (InputPosLong[2] != MatchLong[2])
                                { InputPos += 8; Match += 8; goto FinishMatch; }
                                if (InputPosLong[3] != MatchLong[3])
                                { InputPos += 12; Match += 12; goto FinishMatch; }
                                if (InputPosLong[4] != MatchLong[4])
                                { InputPos += 16; Match += 16; goto FinishMatch; }
                                if (InputPosLong[5] != MatchLong[5])
                                { InputPos += 20; Match += 20; goto FinishMatch; }
                                if (InputPosLong[6] != MatchLong[6])
                                { InputPos += 24; Match += 24; goto FinishMatch; }
                                if (InputPosLong[7] != MatchLong[7])
                                { InputPos += 28; Match += 28; goto FinishMatch; }
                                InputPos += 32;
                                Match += 32;
                                InputPosLong = (ULONG /* UNALIGNED */ *)InputPos;
                                MatchLong = (ULONG /* UNALIGNED */ *)Match;
                            }

                            while (InputPos < InputBufferEnd &&
                                   InputPos[0] == Match[0])
                            {
                                ++InputPos;
                                ++Match;
                            }
                            goto MatchDone;

                            FinishMatch:
                            if (InputPos[0] != Match[0])
                            { goto MatchDone; }
                            if (InputPos[1] != Match[1])
                            { InputPos += 1; Match += 1; goto MatchDone; }
                            if (InputPos[2] != Match[2])
                            { InputPos += 2; Match += 2; goto MatchDone; }
                            InputPos += 3;
                            Match += 3;

                            MatchDone:

                            NT_ASSERT(InputPos > SavedInputPos);
                            MatchLen = (ULONG_PTR)(InputPos - SavedInputPos);

                            //
                            // Is this match the best we've seen?
                            //

                            if (MatchLen > BestMatchLen)
                            {

                                BestMatchLen = MatchLen;
                                NT_ASSERT(InputPos > Match);
                                MatchOffset = (ULONG_PTR)(InputPos - Match);

                                if (Match > SavedInputPos)
                                {
                                    InputPos = SavedInputPos;
                                    break;
                                }

                            }
                            else
                            {

                                i += MatchLen;
                            }

                            Match -= MatchLen;
                            InputPos = SavedInputPos;
                        }

                        ++i;

                        if (i >= 24)
                        {

                            //
                            // Stop searching after a while so the function doesn't take
                            // too long.
                            //

                            break;
                        }
                    }

                    i = 0;

                    MatchLen = BestMatchLen;
                    BestMatchLen = 3;

                    InputPos += MatchLen;

                    //
                    // Encode the best match.  This part is identical to the code for
                    // the "standard" engine.
                    //

                    NT_ASSERT(MatchOffset <= LZ_WINDOW_SIZE);

                    MatchLen -= 3;
                    MatchOffset -= 1;

                    MatchOffset <<= (int)LZ_SMALL_LEN_BITS;

                    EncodeExtraLen1:

                    if (!gotoEncodeExtraLen && MatchLen < LZ_MAX_SMALL_LEN)
                    {

                        MatchOffset += MatchLen;
                        *((USHORT /* UNALIGNED */ *)OutputPos) = (USHORT)MatchOffset;
                        OutputPos += 2;

                    }
                    else
                    {
                        if (gotoEncodeExtraLen) { goto EncodeExtraLen2; }
                        MatchOffset |= LZ_MAX_SMALL_LEN;
                        *((USHORT /* UNALIGNED */ *)OutputPos) = (USHORT)MatchOffset;
                        OutputPos += 2;

                        MatchLen -= LZ_MAX_SMALL_LEN;

                        EncodeExtraLen2:
                        if (!gotoEncodeExtraLen && LastLenHalfByte == null)
                        {

                            LastLenHalfByte = OutputPos;

                            if (MatchLen < 15)
                            {

                                OutputPos[0] = (UCHAR)MatchLen;
                                ++OutputPos;

                            }
                            else
                            {

                                OutputPos[0] = 15;
                                ++OutputPos;
                                gotoEncodeExtraLen = true;
                                goto EncodeExtraLen1;
                            }

                        }
                        else
                        {
                            if (!gotoEncodeExtraLen && MatchLen < 15)
                            {

                                LastLenHalfByte[0] |= (UCHAR)(MatchLen << 4);
                                LastLenHalfByte = null;

                            }
                            else
                            {
                                if (gotoEncodeExtraLen) { goto EncodeExtraLen; }

                                LastLenHalfByte[0] |= 15 << 4;
                                LastLenHalfByte = null;

                                EncodeExtraLen:
                                gotoEncodeExtraLen = false;

                                MatchLen -= 15;

                                if (MatchLen < 255)
                                {

                                    OutputPos[0] = (UCHAR)MatchLen;
                                    ++OutputPos;

                                }
                                else
                                {

                                    OutputPos[0] = 255;
                                    ++OutputPos;
                                    MatchLen += LZ_MAX_SMALL_LEN + 15;

                                    if (MatchLen < (1 << 16))
                                    {

                                        *((USHORT /* UNALIGNED */ *)OutputPos) =
                                            (USHORT)MatchLen;
                                        OutputPos += 2;

                                    }
                                    else
                                    {

                                        *((USHORT /* UNALIGNED */ *)OutputPos) = 0;
                                        OutputPos += 2;

                                        *((ULONG /* UNALIGNED */ *)OutputPos) =
                                            (ULONG)MatchLen;
                                        OutputPos += 4;
                                    }
                                }
                            }
                        }
                    }

                    if ((INT)RunningTags > 0)
                    {

                        RunningTags = RunningTags * 2 + 1;

                    }
                    else
                    {

                        RunningTags = RunningTags * 2 + 1;
                        *TagsOut = RunningTags;
                        RunningTags = 1;
                        TagsOut = (ULONG /* UNALIGNED */ *)OutputPos;
                        OutputPos += sizeof(ULONG);
                    }

                    if (OutputPos >= SafeOutputBufferEnd)
                    {
                        goto SafeDone;
                    }
                }

                HashWindowDone:

                //
                // We finished the current hash window.  If we're at the end of the
                // buffer, break out of the loop.  Otherwise, do more hashing.
                //

                if (InputPos >= SafeBufferEnd)
                {
                    break;
                }
            }

            SafeDone:

            while (InputPos < InputBufferEnd && OutputPos < OutputBufferEnd)
            {

                OutputPos[0] = InputPos[0];
                ++OutputPos;
                ++InputPos;

                if ((INT)RunningTags > 0)
                {

                    RunningTags *= 2;

                }
                else
                {

                    RunningTags *= 2;
                    *TagsOut = RunningTags;
                    RunningTags = 1;
                    TagsOut = (ULONG /* UNALIGNED */ *)OutputPos;
                    OutputPos += sizeof(ULONG);
                }
            }

            if (OutputPos >= OutputBufferEnd)
            {

                return STATUS_BUFFER_TOO_SMALL;
            }

            while ((INT)RunningTags > 0)
            {
                RunningTags = RunningTags * 2 + 1;
            }

            RunningTags = RunningTags * 2 + 1;

            *TagsOut = RunningTags;

            *FinalCompressedSize = (ULONG)(OutputPos - CompressedBuffer);

            if (*FinalCompressedSize < 8)
            {
                *FinalCompressedSize = 8;
            }

            return STATUS_SUCCESS;
        }

        [CLSCompliant(false)]
        public static unsafe NTSTATUS RtlDecompressBufferXpressLz(
                byte[] uncompressedBuffer,
                byte[] compressedBuffer,
                uint compressedBufferSize,
                out uint finalUncompressedSize)
        {
            fixed (UCHAR* UncompressedBuffer = uncompressedBuffer)
            fixed (UCHAR* CompressedBuffer = compressedBuffer)
            fixed (ULONG* FinalUncompressedSize = &finalUncompressedSize)
            {
                return RtlDecompressBufferXpressLz(
                    UncompressedBuffer,
                    (ULONG)uncompressedBuffer.Length,
                    CompressedBuffer,
                    (ULONG)compressedBufferSize,
                    FinalUncompressedSize);
            }
        }


        private static unsafe NTSTATUS
RtlDecompressBufferXpressLz(
   /* OUT */ UCHAR* UncompressedBuffer,
    /* IN */ ULONG UncompressedBufferSize,
    /* IN */ UCHAR* CompressedBuffer,
    /* IN */ ULONG CompressedBufferSize,
   /* OUT */ ULONG* FinalUncompressedSize
    )

        /*++

        Routine Description:

            This routine takes as input a compressed buffer and produces its
            uncompressed equivalent provided the uncompressed data fits within the
            specified destination buffer.

            An output variable indicates the number of bytes used to store the
            uncompressed data.

        Arguments:

            UncompressedBuffer - Supplies a pointer to where the uncompressed data is to
                be stored.

            UncompressedBufferSize - Supplies the size, in bytes, of the uncompressed
                buffer.

            CompressedBuffer - Supplies a pointer to the compressed data.

            CompressedBufferSize - Supplies the size, in bytes, of the compressed
                buffer.

            UncompressedChunkSize - Not meaningful for this algorithm.

            FinalUncompressedSize - Receives the number of bytes needed in the
                uncompressed buffer to store the uncompressed data.

            WorkspacePvoid - Ignored.  Should be NULL.

        Return Value:

            STATUS_SUCCESS - Success.

            STATUS_BAD_COMPRESSION_BUFFER - the input compressed buffer is ill-formed.

        --*/

        {
            UCHAR* InputEnd;
            UCHAR* OutputEnd;
            UCHAR* SafeInputEnd;
            UCHAR* SafeOutputEnd;
            UCHAR* InputPos;
            UCHAR* OutputPos;
            UCHAR* MatchSrc;
            UCHAR* LastLenHalfByte;
            LONG_PTR MatchLen = default(LONG_PTR);
            ULONG_PTR MatchOffset = default(ULONG_PTR);
            INT Tags = default(INT);

            if (CompressedBufferSize < 5)
            {
                return STATUS_BAD_COMPRESSION_BUFFER;
            }

            InputEnd = CompressedBuffer + CompressedBufferSize;
            OutputEnd = UncompressedBuffer + UncompressedBufferSize;

            InputPos = CompressedBuffer;
            OutputPos = UncompressedBuffer;

            //
            // The SafeInputEnd lets us decode 32 2.5-byte matches and one 4-byte tag
            // block without checking bounds.  In other words, it lets us decode an
            // entire tag block (as long as it doesn't contain long matches) without
            // checking bounds.  This way, we only check the safe bound when we load a
            // new tag block.  Similarly, the SafeOutputEnd lets us decode an entire tag
            // block of short matches (or literals) without checking bounds.
            //

            SafeInputEnd = InputEnd - 33 * 5 / 2 - 4;
            SafeOutputEnd = OutputEnd - 11 * 32;

            LastLenHalfByte = null; ;

            bool gotoGetNextTags = false;
            bool gotoSafeDecodeNewTagEntry = false;
            bool gotoProcessLiteral = false;
            bool gotoSafeDecodeLongLen = false;
            bool gotoProcessLiteralSafe = false;

            gotoGetNextTags = true; //goto GetNextTags;

            for (; ; )
            {
                if (gotoGetNextTags)
                {
                    goto GetNextTags1;
                }

            ProcessLiteral1:

                for (; ; )
                {
                    if (gotoProcessLiteral)
                    {
                        goto ProcessLiteral;
                    }

                    //
                    // if (Tags < 0) is a way of checking if the high-bit is set.  This
                    // indicates the next thing is a match.
                    //

                    if (Tags < 0)
                    {
                        break;
                    }

                    //
                    // The next thing is a literal.  Shift the tags to check the next
                    // high bit.
                    //

                    Tags *= 2;

                ProcessLiteral:
                    gotoProcessLiteral = false;

                    if (Tags < 0)
                    {

                        //
                        // The next thing is a match, but we haven't copied the previous
                        // literal yet.  Do that now, then process the match.
                        //

                        OutputPos[0] = InputPos[0];
                        ++OutputPos;
                        ++InputPos;
                        break;
                    }

                    Tags *= 2;

                    if (Tags < 0)
                    {

                        //
                        // Copy the previous two literals.
                        //

                        *((USHORT /* UNALIGNED */ *)OutputPos) = *((USHORT /* UNALIGNED */ *)InputPos);
                        OutputPos += 2;
                        InputPos += 2;
                        break;
                    }

                    Tags *= 2;

                    if (Tags < 0)
                    {

                        //
                        // Copy the previous three literals.
                        //

                        *((ULONG /* UNALIGNED */ *)OutputPos) = *((ULONG /* UNALIGNED */ *)InputPos);
                        OutputPos += 3;
                        InputPos += 3;
                        break;
                    }

                    Tags *= 2;

                    //
                    // We know we need to copy four literals whether or not the next
                    // thing is a match or a literal.
                    //

                    *((ULONG /* UNALIGNED */ *)OutputPos) = *((ULONG /* UNALIGNED */ *)InputPos);
                    OutputPos += 4;
                    InputPos += 4;

                    if (Tags < 0)
                    {
                        break;
                    }

                    Tags *= 2;

                    //
                    // Go back to the state where we have one "delayed" literal.
                    //

                    goto ProcessLiteral;
                }

                Tags *= 2;

                //
                // We're here because a bit was set in the tags.  This means one of two
                // things: either there is a match, or we finished processing the
                // current tag block.  If the latter is true, the tags must now be equal
                // to zero.
                //

                GetNextTags1:

                if (gotoGetNextTags || Tags == 0)
                {

                    //GetNextTags:
                    gotoGetNextTags = false;

                    //
                    // Load the next 32 tags.
                    //

                    //#if defined(_ARM_) || defined(_ARM64_)
                    //            __prefetch(InputPos + 48);
                    //            __prefetch(InputPos + 64);
                    //#endif

                    Tags = *((INT /* UNALIGNED */ *)InputPos);
                    InputPos += 4;

                    //
                    // Check our safe bounds to make sure we can process this tag block
                    // without checking bounds (except when we encounter long-length
                    // matches).
                    //

                    if (InputPos >= SafeInputEnd ||
                        OutputPos >= SafeOutputEnd)
                    {
                        gotoSafeDecodeNewTagEntry = true;
                        goto SafeDecodeNewTagEntry1;
                    }

                    //
                    // Test the high bit of the new tags and shift a '1' to the bottom
                    // so we'll break out of the literal-copying loop when we finish
                    // this set of tags.
                    //

                    if (Tags < 0)
                    {
                        Tags = Tags * 2 + 1;
                    }
                    else
                    {
                        Tags = Tags * 2 + 1;
                        gotoProcessLiteral = true;
                        goto ProcessLiteral1;
                    }
                }

                //
                // Decode the offset and length for this match.
                //

                MatchOffset = *((USHORT /* UNALIGNED */ *)InputPos);
                InputPos += 2;
                MatchLen = (LONG_PTR)(MatchOffset & LZ_MAX_SMALL_LEN);

                MatchOffset >>= (LONG)LZ_SMALL_LEN_BITS;
                MatchOffset += 1;

                if (MatchLen == LZ_MAX_SMALL_LEN)
                {

                    //
                    // This is a longer match.  Read the longer length from the next 4
                    // bits.  We still don't need to check the input bound for this.
                    //

                    if (LastLenHalfByte == null)
                    {

                        LastLenHalfByte = InputPos;
                        ++InputPos;

                        MatchLen = LastLenHalfByte[0] & ((1 << 4) - 1);

                    }
                    else
                    {

                        MatchLen = LastLenHalfByte[0] >> 4;
                        LastLenHalfByte = null;
                    }

                    if (MatchLen == (1 << 4) - 1)
                    {

                        //
                        // The extra 4 bits indicate that the match is even longer.
                        // Check the next byte.  Now we must check the safe bound
                        // because if we hit this case for all 32 entries of the tag
                        // block, we would advance the InputPos 32 * 3.5, and we didn't
                        // allow that much space in our "safe" check.
                        //

                        if (InputPos + 7 >= SafeInputEnd)
                        {
                            gotoSafeDecodeLongLen = true;
                            goto SafeDecodeLongLen1;
                        }

                        MatchLen = InputPos[0];
                        ++InputPos;

                        if (MatchLen == 255)
                        {

                            //
                            // The additional two-byte length case.
                            //

                            MatchLen = *((USHORT /* UNALIGNED */ *)InputPos);
                            InputPos += 2;

                            if (MatchLen == 0)
                            {

                                MatchLen = *((ULONG /* UNALIGNED */ *)InputPos);
                                InputPos += 4;
                            }

                            if (MatchLen < (1 << 4) - 1 + LZ_MAX_SMALL_LEN ||
                                OutputPos + MatchLen + 3 < OutputPos)
                            {
                                return STATUS_BAD_COMPRESSION_BUFFER;
                            }

                            MatchLen -= (1 << 4) - 1 + LZ_MAX_SMALL_LEN;
                        }

                        MatchLen += (1 << 4) - 1;
                    }

                    MatchLen += LZ_MAX_SMALL_LEN;
                }

                MatchLen += 3;

                //
                // Subtract the offset from the current position and make sure this
                // falls inside the buffer.
                //

                MatchSrc = OutputPos - MatchOffset;

                if (MatchSrc < UncompressedBuffer)
                {
                    return STATUS_BAD_COMPRESSION_BUFFER;
                }

                if (MatchOffset < 4)
                {

                    //
                    // When the offset is less than four, copying four bytes at once
                    // doesn't do what we want.  Fix this by copying the first part of
                    // the match, then fall through to the normal case.
                    //

                    switch (MatchOffset)
                    {

                        case 1:
                            OutputPos[0] = MatchSrc[0];
                            OutputPos[1] = MatchSrc[0];
                            OutputPos[2] = MatchSrc[0];
                            MatchLen -= 3;
                            OutputPos += 3;
                            break;
                        case 2:
                            OutputPos[0] = MatchSrc[0];
                            OutputPos[1] = MatchSrc[1];
                            MatchLen -= 2;
                            OutputPos += 2;
                            break;
                        case 3:
                            OutputPos[0] = MatchSrc[0];
                            OutputPos[1] = MatchSrc[1];
                            OutputPos[2] = MatchSrc[2];
                            OutputPos += 3;
                            MatchLen -= 3;
                            break;
                        default:
                            //__assume(0);
                            throw new InvalidOperationException();
                    }

                    if (MatchLen == 0)
                    {
                        continue;
                    }
                }

                for (; ; )
                {

                    //
                    // Copy eight bytes then see if that was enough.  Note that the safe
                    // output bound lets us copy short matches without checking for the
                    // end of the output buffer.
                    //

                    *((ULONG /* UNALIGNED */ *)OutputPos) = *((ULONG /* UNALIGNED */ *)MatchSrc);
                    *((ULONG /* UNALIGNED */ *)(OutputPos + 4)) = *((ULONG /* UNALIGNED */ *)(MatchSrc + 4));

                    if (MatchLen < 9)
                    {
                        OutputPos += MatchLen;
                        break;
                    }

                    OutputPos += 8;
                    MatchSrc += 8;
                    MatchLen -= 8;

                    for (; ; )
                    {

                        //
                        // Now we must test the safe bound.
                        //

                        if (OutputPos >= SafeOutputEnd)
                        {

                            if (OutputPos + MatchLen > OutputEnd)
                            {
                                return STATUS_BAD_COMPRESSION_BUFFER;
                            }

                            __movsb(OutputPos, MatchSrc, (ULONG_PTR)MatchLen);
                            OutputPos += MatchLen;

                            goto SafeDecode;
                        }

                        //#if defined(_ARM_) || defined(_ARM64_)
                        //                if (MatchLen > 32) {
                        //                    __prefetch(MatchSrc + 32);
                        //                }
                        //#endif

                        //
                        // Copy 16 bytes at a time so we can process long matches
                        // quickly.
                        //

                        *((ULONG /* UNALIGNED */ *)OutputPos) = *((ULONG /* UNALIGNED */ *)MatchSrc);
                        *((ULONG /* UNALIGNED */ *)(OutputPos + 4)) = *((ULONG /* UNALIGNED */ *)(MatchSrc + 4));
                        *((ULONG /* UNALIGNED */ *)(OutputPos + 8)) = *((ULONG /* UNALIGNED */ *)(MatchSrc + 8));
                        *((ULONG /* UNALIGNED */ *)(OutputPos + 12)) = *((ULONG /* UNALIGNED */ *)(MatchSrc + 12));

                        if (MatchLen < 17)
                        {
                            OutputPos += MatchLen;
                            break;
                        }

                        OutputPos += 16;
                        MatchSrc += 16;
                        MatchLen -= 16;
                    }

                    break;
                }
            }

            SafeDecode:
            SafeDecodeNewTagEntry1:

            //
            // The code below is the "safe" (and slow) version of the above code.  It
            // checks bounds for all its reads and writes.
            //

            SafeDecodeLongLen1:
            ProcessLiteralSafe1:

            for (; ; )
            {

                for (; ; )
                {
                    if (gotoSafeDecodeNewTagEntry)
                    {
                        goto SafeDecodeNewTagEntry2;
                    }
                    else if (gotoSafeDecodeLongLen)
                    {
                        goto SafeDecodeLongLen2;
                    }
                    else if(gotoProcessLiteralSafe)
                    {
                        goto ProcessLiteralSafe;
                    }

                    if (Tags < 0)
                    {
                        break;
                    }

                    Tags *= 2;

                    ProcessLiteralSafe:
                    gotoProcessLiteralSafe = false;

                    if (Tags < 0)
                    {

                        if (InputPos >= InputEnd ||
                            OutputPos >= OutputEnd)
                        {
                            return STATUS_BAD_COMPRESSION_BUFFER;
                        }

                        OutputPos[0] = InputPos[0];
                        ++OutputPos;
                        ++InputPos;
                        break;
                    }

                    Tags *= 2;

                    if (InputPos + 2 > InputEnd ||
                        OutputPos + 2 > OutputEnd)
                    {
                        return STATUS_BAD_COMPRESSION_BUFFER;
                    }

                    *((USHORT /* UNALIGNED */ *)OutputPos) = *((USHORT /* UNALIGNED */ *)InputPos);
                    OutputPos += 2;
                    InputPos += 2;

                    if (Tags < 0)
                    {
                        break;
                    }

                    Tags *= 2;

                    goto ProcessLiteralSafe;
                }

                Tags *= 2;

                SafeDecodeNewTagEntry2:

                if (gotoSafeDecodeNewTagEntry || Tags == 0)
                {
                    if (gotoSafeDecodeNewTagEntry)
                    {
                        goto SafeDecodeNewTagEntry;
                    }

                    if (InputPos + 3 >= InputEnd)
                    {
                        return STATUS_BAD_COMPRESSION_BUFFER;
                    }

                    Tags = *((INT /* UNALIGNED */ *)InputPos);
                    InputPos += 4;

                    SafeDecodeNewTagEntry:
                    gotoSafeDecodeNewTagEntry = false;

                    if (Tags < 0)
                    {
                        Tags = Tags * 2 + 1;
                    }
                    else
                    {
                        Tags = Tags * 2 + 1;
                        gotoProcessLiteralSafe = true;
                        goto ProcessLiteralSafe1;
                    }
                }

                if (InputPos == InputEnd)
                {
                    break;
                }

                if (InputPos + 1 >= InputEnd)
                {

                    if (OutputPos >= OutputEnd)
                    {

                        //
                        // This is for the case where we said the compressed size was 8,
                        // but it was only 7.
                        //

                        break;
                    }

                    return STATUS_BAD_COMPRESSION_BUFFER;
                }

                MatchOffset = *((USHORT /* UNALIGNED */ *)InputPos);
                InputPos += 2;
                MatchLen = (LONG_PTR)(MatchOffset & LZ_MAX_SMALL_LEN);

                MatchOffset >>= (LONG)LZ_SMALL_LEN_BITS;
                MatchOffset += 1;

                SafeDecodeLongLen2:

                if (gotoSafeDecodeLongLen || MatchLen == LZ_MAX_SMALL_LEN)
                {
                    if(gotoSafeDecodeLongLen)
                    {
                        goto SafeDecodeLongLen3;
                    }

                    if (LastLenHalfByte == null)
                    {

                        if (InputPos >= InputEnd)
                        {
                            return STATUS_BAD_COMPRESSION_BUFFER;
                        }

                        LastLenHalfByte = InputPos;
                        ++InputPos;

                        MatchLen = LastLenHalfByte[0] & ((1 << 4) - 1);

                    }
                    else
                    {

                        MatchLen = LastLenHalfByte[0] >> 4;
                        LastLenHalfByte = null;
                    }

                    SafeDecodeLongLen3:

                    if (gotoSafeDecodeLongLen || MatchLen == (1 << 4) - 1)
                    {

                        //SafeDecodeLongLen:
                        gotoSafeDecodeLongLen = false;

                        if (InputPos >= InputEnd)
                        {
                            return STATUS_BAD_COMPRESSION_BUFFER;
                        }

                        MatchLen = InputPos[0];
                        ++InputPos;

                        if (MatchLen == 255)
                        {

                            if (InputPos + 1 >= InputEnd)
                            {
                                return STATUS_BAD_COMPRESSION_BUFFER;
                            }

                            MatchLen = *((USHORT /* UNALIGNED */ *)InputPos);
                            InputPos += 2;

                            if (MatchLen == 0)
                            {

                                if (InputPos + 3 >= InputEnd)
                                {
                                    return STATUS_BAD_COMPRESSION_BUFFER;
                                }

                                MatchLen = *((ULONG /* UNALIGNED */ *)InputPos);
                                InputPos += 4;
                            }

                            if (MatchLen < (1 << 4) - 1 + LZ_MAX_SMALL_LEN ||
                                OutputPos + MatchLen + 3 < OutputPos)
                            {
                                return STATUS_BAD_COMPRESSION_BUFFER;
                            }

                            MatchLen -= (1 << 4) - 1 + LZ_MAX_SMALL_LEN;
                        }

                        MatchLen += (1 << 4) - 1;
                    }

                    MatchLen += LZ_MAX_SMALL_LEN;
                }

                MatchLen += 3;

                MatchSrc = OutputPos - MatchOffset;

                if (MatchSrc < UncompressedBuffer)
                {
                    return STATUS_BAD_COMPRESSION_BUFFER;
                }

                if (OutputPos + MatchLen > OutputEnd)
                {
                    return STATUS_BAD_COMPRESSION_BUFFER;
                }

                //
                // The movsb instruction seems to be an efficient way to copy in a
                // precise boundary-aware way.
                //

                __movsb(OutputPos, MatchSrc, (ULONG_PTR)MatchLen);
                OutputPos += MatchLen;
            }

            *FinalUncompressedSize = (ULONG)(OutputPos - UncompressedBuffer);

            return STATUS_SUCCESS;
        }
    }
}
