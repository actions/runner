using System;
using System.Diagnostics;

namespace GitHub.Services.Content.Common
{
    [DebuggerNonUserCode]
    public class TaggedUnion<T1, T2> : ITaggedUnion<T1, T2>
    {
        private readonly TaggedUnionValue<T1, T2> impl;

        public TaggedUnion(TaggedUnionValue<T1, T2> toCopy)
        {
            impl = toCopy;
        }

        public TaggedUnion(TaggedUnion<T1, T2> toCopy)
        {
            impl = toCopy.impl;
        }

        public TaggedUnion(T1 value)
        {
            impl = new TaggedUnionValue<T1, T2>(value);
        }

        public TaggedUnion(T2 value)
        {
            impl = new TaggedUnionValue<T1, T2>(value);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2)
        {
            impl.Match(onT1, onT2);
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2)
        {
            return impl.Match(onT1, onT2);
        }

        public override string ToString()
        {
            return impl.ToString();
        }
    }

    [DebuggerNonUserCode]
    public class TaggedUnion<T1, T2, T3> : ITaggedUnion<T1, T2, T3>
    {
        private readonly TaggedUnionValue<T1, T2, T3> impl;

        public TaggedUnion(T1 value)
        {
            impl = new TaggedUnionValue<T1, T2, T3>(value);
        }

        public TaggedUnion(T2 value)
        {
            impl = new TaggedUnionValue<T1, T2, T3>(value);
        }

        public TaggedUnion(T3 value)
        {
            impl = new TaggedUnionValue<T1, T2, T3>(value);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3)
        {
            impl.Match(onT1, onT2, onT3);
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3)
        {
            return impl.Match(onT1, onT2, onT3);
        }

        public override string ToString()
        {
            return impl.ToString();
        }
    }

    [DebuggerNonUserCode]
    public class TaggedUnion<T1, T2, T3, T4> : ITaggedUnion<T1, T2, T3, T4>
    {
        private readonly TaggedUnionValue<T1, T2, T3, T4> impl;

        public TaggedUnion(TaggedUnion<T1, T2, T3, T4> toCopy)
        {
            impl = toCopy.impl;
        }

        public TaggedUnion(T1 value)
        {
            impl = new TaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public TaggedUnion(T2 value)
        {
            impl = new TaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public TaggedUnion(T3 value)
        {
            impl = new TaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public TaggedUnion(T4 value)
        {
            impl = new TaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3, Action<T4> onT4)
        {
            impl.Match(onT1, onT2, onT3, onT4);
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3, Func<T4,T> onT4)
        {
            return impl.Match(onT1, onT2, onT3, onT4);
        }

        public override string ToString()
        {
            return impl.ToString();
        }
    }
}
