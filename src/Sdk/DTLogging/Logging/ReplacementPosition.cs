using System;

namespace GitHub.DistributedTask.Logging
{
    internal sealed class ReplacementPosition
    {
        public ReplacementPosition(Int32 start, Int32 length)
        {
            Start = start;
            Length = length;
        }

        public ReplacementPosition(ReplacementPosition copy)
        {
            Start = copy.Start;
            Length = copy.Length;
        }

        public Int32 Start { get; set; }
        public Int32 Length { get; set; }
        public Int32 End
        {
            get
            {
                return Start + Length;
            }
        }
    }
}
