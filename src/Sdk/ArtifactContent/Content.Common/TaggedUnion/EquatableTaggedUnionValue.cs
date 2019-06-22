using System;
using System.Diagnostics;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// A TaggendUnionValue with an equatable instance, and equatable constraints for it's
    /// parameters.
    /// </summary>
    [DebuggerNonUserCode]
    public struct EquatableTaggedUnionValue<T1, T2> : IEquatable<EquatableTaggedUnionValue<T1, T2>>, ITaggedUnion<T1, T2>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
    {
        private readonly TaggedUnionValue<T1, T2> impl;

        public EquatableTaggedUnionValue(T1 t1)
        {
            this.impl = new TaggedUnionValue<T1, T2>(t1);
        }

        public EquatableTaggedUnionValue(T2 t2)
        {
            this.impl = new TaggedUnionValue<T1, T2>(t2);
        }

        public bool Equals(EquatableTaggedUnionValue<T1, T2> other)
        {
            if(this.impl.which != other.impl.which)
            {
                return false;
            }

            return this.Match(
                one => one.Equals(other.impl.one),
                two => two.Equals(other.impl.two));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ (this.impl.which);
                hashCode = (hashCode * 397) ^ this.impl.CallCommonBase((object o) => o.GetHashCode());
                return hashCode;
            } 
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EquatableTaggedUnionValue<T1, T2>))
            {
                return false;
            }
            return this.Equals((EquatableTaggedUnionValue<T1, T2>)obj);
        }

        public static bool operator ==(EquatableTaggedUnionValue<T1, T2> op1, EquatableTaggedUnionValue<T1, T2> op2)
        {
            return op1.Equals(op2);
        }

        public static bool operator !=(EquatableTaggedUnionValue<T1, T2> op1, EquatableTaggedUnionValue<T1, T2> op2)
        {
            return !op1.Equals(op2);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2) => impl.Match(onT1, onT2);

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2) => impl.Match(onT1, onT2);

        public override string ToString()
        {
            return impl.ToString();
        }
    }

    /// <summary>
    /// A TaggendUnionValue with an equatable instance, and equatable constraints for it's
    /// parameters.
    /// </summary>
    [DebuggerNonUserCode]
    public struct EquatableTaggedUnionValue<T1, T2, T3> : IEquatable<EquatableTaggedUnionValue<T1, T2, T3>>, ITaggedUnion<T1, T2, T3>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
        where T3 : IEquatable<T3>
    {
        private readonly TaggedUnionValue<T1, T2, T3> tagged;

        public EquatableTaggedUnionValue(T1 t1)
        {
            this.tagged = new TaggedUnionValue<T1, T2, T3>(t1);
        }

        public EquatableTaggedUnionValue(T2 t2)
        {
            this.tagged = new TaggedUnionValue<T1, T2, T3>(t2);
        }

        public EquatableTaggedUnionValue(T3 t3)
        {
            this.tagged = new TaggedUnionValue<T1, T2, T3>(t3);
        }

        public bool Equals(EquatableTaggedUnionValue<T1, T2, T3> other)
        {
            if(this.tagged.which != other.tagged.which)
            {
                return false;
            }

            return this.Match(
                one => one.Equals(other.tagged.one),
                two => two.Equals(other.tagged.two),
                three => three.Equals(other.tagged.three));
        }

        public static bool operator ==(EquatableTaggedUnionValue<T1, T2, T3> op1, EquatableTaggedUnionValue<T1, T2, T3> op2)
        {
            return op1.Equals(op2);
        }

        public static bool operator !=(EquatableTaggedUnionValue<T1, T2, T3> op1, EquatableTaggedUnionValue<T1, T2, T3> op2)
        {
            return !op1.Equals(op2);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ (this.tagged.which);
                hashCode = (hashCode * 397) ^ this.tagged.CallCommonBase((object o) => o.GetHashCode());
                return hashCode;
            } 
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EquatableTaggedUnionValue<T1, T2, T3>))
            {
                return false;
            }
            return this.Equals((EquatableTaggedUnionValue<T1, T2, T3>)obj);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3) => tagged.Match(onT1, onT2, onT3);

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3) => tagged.Match(onT1, onT2, onT3);
    }

    /// <summary>
    /// A TaggendUnionValue with an equatable instance, and equatable constraints for it's
    /// parameters.
    /// </summary>
    [DebuggerNonUserCode]
    public struct EquatableTaggedUnionValue<T1, T2, T3, T4> : IEquatable<EquatableTaggedUnionValue<T1, T2, T3, T4>>, ITaggedUnion<T1, T2, T3, T4>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
        where T3 : IEquatable<T3>
        where T4 : IEquatable<T4>
    {
        private readonly TaggedUnionValue<T1, T2, T3, T4> tagged;

        public EquatableTaggedUnionValue(T1 t1)
        {
            this.tagged = new TaggedUnionValue<T1, T2, T3, T4>(t1);
        }

        public EquatableTaggedUnionValue(T2 t2)
        {
            this.tagged = new TaggedUnionValue<T1, T2, T3, T4>(t2);
        }

        public EquatableTaggedUnionValue(T3 t3)
        {
            this.tagged = new TaggedUnionValue<T1, T2, T3, T4>(t3);
        }

        public EquatableTaggedUnionValue(T4 t4)
        {
            this.tagged = new TaggedUnionValue<T1, T2, T3, T4>(t4);
        }

        public bool Equals(EquatableTaggedUnionValue<T1, T2, T3, T4> other)
        {
            if(this.tagged.which != other.tagged.which)
            {
                return false;
            }

            return this.Match(
                one => one.Equals(other.tagged.one),
                two => two.Equals(other.tagged.two),
                three => three.Equals(other.tagged.three),
                four => four.Equals(other.tagged.four));
        }

        public static bool operator ==(EquatableTaggedUnionValue<T1, T2, T3, T4> op1, EquatableTaggedUnionValue<T1, T2, T3, T4> op2)
        {
            return op1.Equals(op2);
        }

        public static bool operator !=(EquatableTaggedUnionValue<T1, T2, T3, T4> op1, EquatableTaggedUnionValue<T1, T2, T3, T4> op2)
        {
            return !op1.Equals(op2);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ (this.tagged.which);
                hashCode = (hashCode * 397) ^ this.tagged.CallCommonBase((object o) => o.GetHashCode());
                return hashCode;
            } 
        }

        public override bool Equals(object obj)
        {
            if ( !(obj is EquatableTaggedUnionValue<T1, T2, T3, T4>))
            {
                return false;
            }
            return this.Equals((EquatableTaggedUnionValue<T1, T2, T3, T4>)obj);
        }

        public void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3, Action<T4> onT4) => tagged.Match(onT1, onT2, onT3, onT4);

        public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3, Func<T4, T> onT4) => tagged.Match(onT1, onT2, onT3, onT4);
    }
}
