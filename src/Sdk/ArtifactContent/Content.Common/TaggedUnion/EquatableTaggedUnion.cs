using System;
using System.Diagnostics;

namespace GitHub.Services.Content.Common
{
    [DebuggerNonUserCode]
    public class EquatableTaggedUnion<T1, T2> : IEquatable<EquatableTaggedUnion<T1, T2>>, ITaggedUnion<T1, T2>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
    {
        private readonly EquatableTaggedUnionValue<T1, T2> impl;

        public EquatableTaggedUnion(EquatableTaggedUnionValue<T1, T2> toCopy)
        {
            impl = toCopy;
        }

        public EquatableTaggedUnion(EquatableTaggedUnion<T1, T2> toCopy)
        {
            impl = toCopy.impl;
        }

        public EquatableTaggedUnion(T1 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2>(value);
        }

        public EquatableTaggedUnion(T2 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2>(value);
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

        public bool Equals(EquatableTaggedUnion<T1, T2> other) 
            => object.ReferenceEquals(other, null) ? false : impl.Equals(other.impl);

        public override bool Equals(object obj)
            => Equals(obj as EquatableTaggedUnion<T1, T2>);

        public static bool operator ==(EquatableTaggedUnion<T1,T2> r1, EquatableTaggedUnion<T1,T2> r2)
            => object.ReferenceEquals(r1, null) ? object.ReferenceEquals(r2, null) : r1.Equals(r2);

        public static bool operator !=(EquatableTaggedUnion<T1,T2> r1, EquatableTaggedUnion<T1,T2> r2) => !(r1 == r2);

        public override int GetHashCode() => impl.GetHashCode();
    }

    [DebuggerNonUserCode]
    public class EquatableTaggedUnion<T1, T2, T3> : IEquatable<EquatableTaggedUnion<T1, T2, T3>>, ITaggedUnion<T1, T2, T3>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
        where T3 : IEquatable<T3>
    {
        private readonly EquatableTaggedUnionValue<T1, T2, T3> impl;

        public EquatableTaggedUnion(EquatableTaggedUnionValue<T1, T2, T3> toCopy)
        {
            impl = toCopy;
        }

        public EquatableTaggedUnion(EquatableTaggedUnion<T1, T2, T3> toCopy)
        {
            impl = toCopy.impl;
        }

        public EquatableTaggedUnion(T1 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2, T3>(value);
        }

        public EquatableTaggedUnion(T2 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2, T3>(value);
        }

        public EquatableTaggedUnion(T3 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2, T3>(value);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3)
        {
            impl.Match(onT1, onT2, onT3);
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3)
        {
            return impl.Match(onT1, onT2, onT3);
        }

        public bool Equals(EquatableTaggedUnion<T1, T2, T3> other) 
            => object.ReferenceEquals(other, null) ? false : impl.Equals(other.impl);

        public override bool Equals(object obj)
            => Equals(obj as EquatableTaggedUnion<T1, T2, T3>);

        public static bool operator ==(EquatableTaggedUnion<T1, T2, T3> r1, EquatableTaggedUnion<T1, T2, T3> r2)
            => object.ReferenceEquals(r1, null) ? object.ReferenceEquals(r2, null) : r1.Equals(r2);

        public static bool operator !=(EquatableTaggedUnion<T1, T2, T3> r1, EquatableTaggedUnion<T1, T2, T3> r2) => !(r1 == r2);

        public override int GetHashCode() => impl.GetHashCode();
    }

    [DebuggerNonUserCode]
    public class EquatableTaggedUnion<T1, T2, T3, T4> : IEquatable<EquatableTaggedUnion<T1, T2, T3, T4>>, ITaggedUnion<T1, T2, T3, T4>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
        where T3 : IEquatable<T3>
        where T4 : IEquatable<T4>
    {
        private readonly EquatableTaggedUnionValue<T1, T2, T3, T4> impl;

        public EquatableTaggedUnion(EquatableTaggedUnionValue<T1, T2, T3, T4> toCopy)
        {
            impl = toCopy;
        }

        public EquatableTaggedUnion(EquatableTaggedUnion<T1, T2, T3, T4> toCopy)
        {
            impl = toCopy.impl;
        }

        public EquatableTaggedUnion(T1 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public EquatableTaggedUnion(T2 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public EquatableTaggedUnion(T3 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public EquatableTaggedUnion(T4 value)
        {
            impl = new EquatableTaggedUnionValue<T1, T2, T3, T4>(value);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3, Action<T4> onT4)
        {
            impl.Match(onT1, onT2, onT3, onT4);
        }

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3, Func<T4, T> onT4)
        {
            return impl.Match(onT1, onT2, onT3, onT4);
        }

        public bool Equals(EquatableTaggedUnion<T1, T2, T3, T4> other) 
            => object.ReferenceEquals(other, null) ? false : impl.Equals(other.impl);

        public override bool Equals(object obj) 
            => Equals(obj as EquatableTaggedUnion<T1, T2, T3, T4>);

        public static bool operator ==(EquatableTaggedUnion<T1, T2, T3, T4> r1, EquatableTaggedUnion<T1, T2, T3, T4> r2)
            => object.ReferenceEquals(r1, null) ? object.ReferenceEquals(r2, null) : r1.Equals(r2);

        public static bool operator !=(EquatableTaggedUnion<T1, T2, T3, T4> r1, EquatableTaggedUnion<T1, T2, T3, T4> r2) => !(r1 == r2);

        public override int GetHashCode() => impl.GetHashCode();
    }
}
