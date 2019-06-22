using System;
using System.Diagnostics;

namespace GitHub.Services.Content.Common
{
    [DebuggerNonUserCode]
    public struct TaggedUnionValue<T1, T2> : ITaggedUnion<T1, T2>
    {
        internal readonly byte which;
        internal readonly T1 one;
        internal readonly T2 two;

        public TaggedUnionValue(TaggedUnionValue<T1, T2> toCopy)
        {
            this.which = toCopy.which;
            this.one = toCopy.one;
            this.two = toCopy.two;
        }

        public TaggedUnionValue(T1 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 0;
            this.one = value;
            this.two = default(T2);
        }

        public TaggedUnionValue(T2 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 1;
            this.one = default(T1);
            this.two = value;
        }

        public void Match(Action<T1> onT1, Action<T2> onT2)
        {
            switch (which)
            {
                case 0:
                    onT1(this.one);
                    break;
                case 1:
                    onT2(this.two);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2)
        {
            switch (which)
            {
                case 0:
                    return onT1(this.one);
                case 1:
                    return onT2(this.two);
                default:
                    throw new InvalidOperationException();
            }
        }

        public override string ToString()
        {
            return this.CallCommonBase((object x) => x.ToString());
        }
    }

    [DebuggerNonUserCode]
    public struct TaggedUnionValue<T1, T2, T3> : ITaggedUnion<T1, T2, T3>
    {
        internal readonly byte which;
        internal readonly T1 one;
        internal readonly T2 two;
        internal readonly T3 three;

        public TaggedUnionValue(TaggedUnionValue<T1, T2, T3> toCopy)
        {
            this.which = toCopy.which;
            this.one = toCopy.one;
            this.two = toCopy.two;
            this.three = toCopy.three;
        }

        public TaggedUnionValue(T1 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 0;
            this.one = value;
            this.two = default(T2);
            this.three = default(T3);
        }

        public TaggedUnionValue(T2 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 1;
            this.one = default(T1);
            this.two = value;
            this.three = default(T3);
        }

        public TaggedUnionValue(T3 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 2;
            this.one = default(T1);
            this.two = default(T2);
            this.three = value;
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3)
        {
            switch (which)
            {
                case 0:
                    onT1(this.one);
                    break;
                case 1:
                    onT2(this.two);
                    break;
                case 2:
                    onT3(this.three);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3)
        {
            switch (which)
            {
                case 0:
                    return onT1(this.one);
                case 1:
                    return onT2(this.two);
                case 2:
                    return onT3(this.three);
                default:
                    throw new InvalidOperationException();
            }
        }

        public override string ToString()
        {
            return this.CallCommonBase((object x) => x.ToString());
        }
    }

    [DebuggerNonUserCode]
    public struct TaggedUnionValue<T1, T2, T3, T4> : ITaggedUnion<T1, T2, T3, T4>
    {
        internal readonly byte which;
        internal readonly T1 one;
        internal readonly T2 two;
        internal readonly T3 three;
        internal readonly T4 four;

        public TaggedUnionValue(TaggedUnionValue<T1, T2, T3, T4> toCopy)
        {
            this.which = toCopy.which;
            this.one = toCopy.one;
            this.two = toCopy.two;
            this.three = toCopy.three;
            this.four = toCopy.four;
        }

        public TaggedUnionValue(T1 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 0;
            this.one = value;
            this.two = default(T2);
            this.three = default(T3);
            this.four = default(T4);
        }

        public TaggedUnionValue(T2 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 1;
            this.one = default(T1);
            this.two = value;
            this.three = default(T3);
            this.four = default(T4);
        }

        public TaggedUnionValue(T3 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 2;
            this.one = default(T1);
            this.two = default(T2);
            this.three = value;
            this.four = default(T4);
        }

        public TaggedUnionValue(T4 value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Tagged unions should not hold a null value.");
            }

            this.which = 3;
            this.one = default(T1);
            this.two = default(T2);
            this.three = default(T3);
            this.four = value;
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3, Action<T4> onT4)
        {
            switch (which)
            {
                case 0:
                    onT1(this.one);
                    break;
                case 1:
                    onT2(this.two);
                    break;
                case 2:
                    onT3(this.three);
                    break;
                case 3:
                    onT4(this.four);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3, Func<T4, T> onT4)
        {
            switch (which)
            {
                case 0:
                    return onT1(this.one);
                case 1:
                    return onT2(this.two);
                case 2:
                    return onT3(this.three);
                case 3:
                    return onT4(this.four);
                default:
                    throw new InvalidOperationException();
            }
        }

        public override string ToString()
        {
            return this.CallCommonBase((object x) => x.ToString());
        }
    }
}
