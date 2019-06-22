using System;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Tagged Union inspired by https://doc.rust-lang.org/book/enums.html
    /// </summary>
    public interface ITaggedUnion<out T1, out T2>
    {
        void Match(Action<T1> onT1, Action<T2> onT2);
        T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2);
    }

    public interface ITaggedUnion<out T1, out T2, out T3>
    {
        void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3);
        T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3);
    }

    public interface ITaggedUnion<out T1, out T2, out T3, out T4>
    {
        void Match(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3, Action<T4> onT4);
        T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3, Func<T4, T> onT4);
    }

    public interface IEquatableTaggedUnion<T1, T2> 
        : ITaggedUnion<T1, T2>, IEquatable<IEquatableTaggedUnion<T1, T2>>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
    {
    }

    public interface IEquatableTaggedUnion<T1, T2, T3> 
        : ITaggedUnion<T1, T2, T3>, IEquatable<IEquatableTaggedUnion<T1, T2, T3>>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
        where T3 : IEquatable<T3>
    {
    }

    public interface IEquatableTaggedUnion<T1, T2, T3, T4> 
        : ITaggedUnion<T1, T2, T3, T4>, IEquatable<IEquatableTaggedUnion<T1, T2, T3, T4>>
        where T1 : IEquatable<T1>
        where T2 : IEquatable<T2>
        where T3 : IEquatable<T3>
        where T4 : IEquatable<T4>
    {
    }
}
