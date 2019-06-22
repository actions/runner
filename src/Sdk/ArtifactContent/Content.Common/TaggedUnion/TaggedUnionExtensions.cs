using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitHub.Services.Content.Common
{
    [DebuggerNonUserCode]
    public static class TaggedUnionExtensions
    {
        public static void CallCommonBase<TBase, T1, T2>(this TaggedUnion<T1, T2> tagged, Action<TBase> useAction)
            where T1 : TBase
            where T2 : TBase
        {
            useAction(tagged.Match<TBase>(one => one, two => two));
        }

        public static T CallCommonBase<T, TBase, T1, T2>(this TaggedUnion<T1, T2> tagged, Func<TBase, T> useAction)
            where T1 : TBase
            where T2 : TBase
        {
            return useAction(tagged.Match<TBase>(one => one, two => two));
        }

        public static void CallCommonBase<TBase, T1, T2, T3>(this TaggedUnion<T1, T2, T3> tagged, Action<TBase> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
        {
            useAction(tagged.Match<TBase>(one => one, two => two, three => three));
        }

        public static T CallCommonBase<T, TBase, T1, T2, T3>(this TaggedUnion<T1, T2, T3> tagged, Func<TBase, T> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
        {
            return useAction(tagged.Match<TBase>(one => one, two => two, three => three));
        }

        public static void CallCommonBase<TBase, T1, T2, T3, T4>(this TaggedUnion<T1, T2, T3, T4> tagged, Action<TBase> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
            where T4 : TBase
        {
            useAction(tagged.Match<TBase>(one => one, two => two, three => three, four => four));
        }

        public static T CallCommonBase<T, TBase, T1, T2, T3, T4>(this TaggedUnion<T1, T2, T3, T4> tagged, Func<TBase, T> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
            where T4 : TBase
        {
            return useAction(tagged.Match<TBase>(one => one, two => two, three => three, four => four));
        }

        // For TaggedUnionValue struct

        public static void CallCommonBase<TBase, T1, T2>(this TaggedUnionValue<T1, T2> tagged, Action<TBase> useAction)
            where T1 : TBase
            where T2 : TBase
        {
            useAction(tagged.Match<TBase>(one => one, two => two));
        }

        public static T CallCommonBase<T, TBase, T1, T2>(this TaggedUnionValue<T1, T2> tagged, Func<TBase, T> useAction)
            where T1 : TBase
            where T2 : TBase
        {
            return useAction(tagged.Match<TBase>(one => one, two => two));
        }

        public static void CallCommonBase<TBase, T1, T2, T3>(this TaggedUnionValue<T1, T2, T3> tagged, Action<TBase> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
        {
            useAction(tagged.Match<TBase>(one => one, two => two, three => three));
        }

        public static T CallCommonBase<T, TBase, T1, T2, T3>(this TaggedUnionValue<T1, T2, T3> tagged, Func<TBase, T> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
        {
            return useAction(tagged.Match<TBase>(one => one, two => two, three => three));
        }

        public static void CallCommonBase<TBase, T1, T2, T3, T4>(this TaggedUnionValue<T1, T2, T3, T4> tagged, Action<TBase> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
            where T4 : TBase
        {
            useAction(tagged.Match<TBase>(one => one, two => two, three => three, four => four));
        }

        public static T CallCommonBase<T, TBase, T1, T2, T3, T4>(this TaggedUnionValue<T1, T2, T3, T4> tagged, Func<TBase, T> useAction)
            where T1 : TBase
            where T2 : TBase
            where T3 : TBase
            where T4 : TBase
        {
            return useAction(tagged.Match<TBase>(one => one, two => two, three => three, four => four));
        }

        /* ****************************************************************** */
        /* Testing of the type of content */

        public static bool IsOne<T, T1, T2>(this T tagged) where T : ITaggedUnion<T1, T2>
        {
            return tagged.Match(one => true, two => false);
        }

        public static bool IsOne<T, T1, T2, T3>(this T tagged) where T : ITaggedUnion<T1, T2, T3>
        {
            return tagged.Match(one => true, two => false, three => false);
        }

        public static bool IsOne<T, T1, T2, T3, T4>(this T tagged) where T : ITaggedUnion<T1, T2, T3, T4> 
        {
            return tagged.Match(one => true, two => false, three => false, four => false);
        }

        public static bool IsTwo<T, T1, T2>(this T tagged) where T : ITaggedUnion<T1, T2> 
        {
            return tagged.Match(one => false, two => true);
        }

        public static bool IsTwo<T, T1, T2, T3>(this T tagged) where T : ITaggedUnion<T1, T2, T3> 
        {
            return tagged.Match(one => false, two => true, three => false);
        }

        public static bool IsTwo<T, T1, T2, T3, T4>(this T tagged) where T : ITaggedUnion<T1, T2, T3, T4> 
        {
            return tagged.Match(one => false, two => true, three => false, four => false);
        }

        public static bool IsThree<T, T1, T2, T3>(this T tagged) where T : ITaggedUnion<T1, T2, T3> 
        {
            return tagged.Match(one => false, two => false, three => true);
        }

        public static bool IsThree<T, T1, T2, T3, T4>(this T tagged) where T : ITaggedUnion<T1, T2, T3, T4>
        {
            return tagged.Match(one => false, two => true, three => true, four => false);
        }

        public static bool IsFour<T, T1, T2, T3, T4>(this T tagged) where T : ITaggedUnion<T1, T2, T3, T4>
        {
            return tagged.Match(one => false, two => true, three => false, four => true);
        }

        /* ****************************************************************** */
        /* Discriminated Selection */


        /* Select Ones */

        public static IEnumerable<T1> SelectOnes<T1, T2>(this IEnumerable<ITaggedUnion<T1, T2>> taggedEnumerable)
        {
            return SelectOnes(taggedEnumerable, x => x);
        }

        public static IEnumerable<R> SelectOnes<T1, T2, R>(this IEnumerable<ITaggedUnion<T1, T2>> taggedEnumerable, Func<T1, R> selector)
        {
           return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { selector(one) } , two => new R[] { }));
        }

        public static IEnumerable<T1> SelectOnes<T1, T2, T3>(this IEnumerable<ITaggedUnion<T1, T2, T3>> taggedEnumerable)
        {
            return SelectOnes(taggedEnumerable, x => x);
        }

        public static IEnumerable<R> SelectOnes<T1, T2, T3, R>(this IEnumerable<ITaggedUnion<T1, T2, T3>> taggedEnumerable, Func<T1, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { selector(one) } , two => new R[] { }, three => new R[] { }));
        }

        public static IEnumerable<T1> SelectOnes<T1, T2, T3, T4>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable)
        {
            return SelectOnes(taggedEnumerable, x => x);
        }

        public static IEnumerable<R> SelectOnes<T1, T2, T3, T4, R>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable, Func<T1, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { selector(one) } , two => new R[] { }, three => new R[] { }, four => new R[] { }));
        }

        /* Select Twos */

        public static IEnumerable<T2> SelectTwos<T1, T2>(this IEnumerable<ITaggedUnion<T1, T2>> taggedEnumerable)
        {
            return SelectTwos(taggedEnumerable, x => x);
        }

        public static IEnumerable<R> SelectTwos<T1, T2, R>(this IEnumerable<ITaggedUnion<T1, T2>> taggedEnumerable, Func<T2, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { } , two => new R[] { selector(two) }));
        }

        public static IEnumerable<T2> SelectTwos<T1, T2, T3>(this IEnumerable<ITaggedUnion<T1, T2, T3>> taggedEnumerable)
        {
            return SelectTwos(taggedEnumerable, x => x);
        }

        public static IEnumerable<R> SelectTwos<T1, T2, T3, R>(this IEnumerable<ITaggedUnion<T1, T2, T3>> taggedEnumerable, Func<T2, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { } , two => new R[] { selector(two) }, three => new R[] { }));
        }

        public static IEnumerable<T2> SelectTwos<T1, T2, T3, T4>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable)
        {
            return SelectTwos(taggedEnumerable, x => x);
        }

        public static IEnumerable<R> SelectTwos<T1, T2, T3, T4, R>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable, Func<T2, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { } , two => new R[] { selector(two) }, three => new R[] { }, four => new R[] { }));
        }

        /* Select Threes */

        public static IEnumerable<T3> SelectThrees<T1, T2, T3>(this IEnumerable<ITaggedUnion<T1, T2, T3>> taggedEnumerable)
        {
            return SelectThrees(taggedEnumerable, x => x);
        }

        public static IEnumerable<R> SelectThrees<T1, T2, T3, R>(this IEnumerable<ITaggedUnion<T1, T2, T3>> taggedEnumerable, Func<T3, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { } , two => new R[] { }, three => new R[] { selector(three) }));
        }

        public static IEnumerable<T3> SelectThrees<T1, T2, T3, T4>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable)
        {
            return SelectThrees(taggedEnumerable, x => x);
        }
        public static IEnumerable<R> SelectThrees<T1, T2, T3, T4, R>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable, Func<T3, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { } , two => new R[] { }, three => new R[] { selector(three) }, four => new R[] { }));
        }

        public static IEnumerable<T4> SelectFours<T1, T2, T3, T4>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable)
        {
            return SelectFours(taggedEnumerable, x => x);
        }

        /* Select Fours */

        public static IEnumerable<R> SelectFours<T1, T2, T3, T4, R>(this IEnumerable<ITaggedUnion<T1, T2, T3, T4>> taggedEnumerable, Func<T4, R> selector)
        {
            return taggedEnumerable.SelectMany(x => x.Match(one => new R[] { } , two => new R[] { }, three => new R[] { }, four => new R[] { selector(four) }));
        }
    }
}
