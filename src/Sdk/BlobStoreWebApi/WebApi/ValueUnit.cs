// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    ///     This type is effectively 'void', but usable as a type parameter when a value type is needed.
    /// </summary>
    /// <remarks>
    ///     This is useful for generic methods dealing in tasks, since one can avoid having an overload
    ///     for both Task and Task{TResult}. One instead provides only a Task{TResult} overload, and
    ///     callers with a void result return Void.
    /// </remarks>
    [SuppressMessage(
        "Microsoft.Performance",
        "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes",
        Justification = "There is no point in comparing or hashing ValueUnit.")]
    public struct ValueUnit
    {
        /// <summary>
        ///     Void unit type
        /// </summary>
        public static readonly ValueUnit Void = default(ValueUnit);
    }
}
