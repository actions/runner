// Copyright 2012 Netflix, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    /// <summary>
    /// A base implementation for immutable keys to represent objects.
    /// Keys are equal if their types are the same and their names are equal with ordinal string comparison.
    /// </summary>
    public abstract class ImmutableKey : IEquatable<ImmutableKey>
    {
        /// <summary>
        /// Readonly field to store the name of the key.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableKey"/> class.
        /// </summary>
        /// <param name="name">The name of the key, can't be null or whitespace.</param>
        protected ImmutableKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            this.name = name;
        }

        /// <summary>
        /// Gets the name of the key.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Determines whether two specified keys are equal.
        /// </summary>
        /// <param name="left">The first key to compare.</param>
        /// <param name="right">The second key to compare.</param>
        /// <returns>True if the two keys are equal.</returns>
        public static bool operator ==(ImmutableKey left, ImmutableKey right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified keys are different.
        /// </summary>
        /// <param name="left">The first key to compare.</param>
        /// <param name="right">The second key to compare.</param>
        /// <returns>True if the two keys are not equal.</returns>
        public static bool operator !=(ImmutableKey left, ImmutableKey right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="ImmutableKey"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return Equals(obj as ImmutableKey);
        }

        /// <summary>
        /// Calculates the hash code for this key.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }

        /// <summary>
        /// Returns the name of the current key.
        /// </summary>
        /// <returns>The name of the command key.</returns>
        public override string ToString()
        {
            return this.name;
        }

        /// <summary>
        /// Determines whether the specified <see cref="ImmutableKey"/> is equal to the current <see cref="ImmutableKey"/>.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false</returns>
        public bool Equals(ImmutableKey other)
        {
            if (object.ReferenceEquals(other, null) || GetType() != other.GetType())
            {
                return false;
            }

            return this.name.Equals(other.name, StringComparison.Ordinal);
        }
    }

    
    
    /// <summary>
    /// An immutable key to represent a <see cref="Command"/> for monitoring, circuit-breakers,
    /// metrics publishing, caching and other such uses.
    /// Command keys are equal if their names are equal with ordinal string comparison.
    /// </summary>
    public class CommandKey : ImmutableKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandKey"/> class.
        /// </summary>
        /// <param name="name">The name of the command key.</param>
        public CommandKey(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Converts a string to a <see cref="CommandKey"/> object.
        /// </summary>
        /// <param name="name">The name of the command key.</param>
        /// <returns>A <see cref="CommandKey"/> object constructed from the specified name.</returns>
        public static implicit operator CommandKey(string name)
        {
            return new CommandKey(name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandKey"/> class based on the type of the command.
        /// This used to create default command keys for unnamed commands.
        /// </summary>
        /// <param name="commandType">The type of the command.</param>
        public CommandKey(Type commandType)
            : base(GetDefaultNameForCommandType(commandType))
        {
        }

        /// <summary>
        /// Gets the default name for a command type.
        /// </summary>
        /// <param name="commandType">The command type.</param>
        /// <returns>The default name for the command type.</returns>
        private static string GetDefaultNameForCommandType(Type commandType)
        {
            if (commandType == null)
            {
                throw new ArgumentNullException("commandType");
            }

            return commandType.Name;
        }
    }



    /// <summary>
    /// A group name for a <see cref="Command"/>. This is used to represent a common relationship between commands. For example, a library 
    /// or team name, the system all related commands interact with, common business purpose etc.
    /// </summary>
    public class CommandGroupKey : ImmutableKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandGroupKey"/> class.
        /// </summary>
        /// <param name="name">The name of the command group key.</param>
        public CommandGroupKey(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Converts a string to a <see cref="CommandGroupKey"/> object.
        /// </summary>
        /// <param name="name">The name of the command group key.</param>
        /// <returns>A <see cref="CommandGroupKey"/> object constructed from the specified name.</returns>
        public static implicit operator CommandGroupKey(string name)
        {
            return new CommandGroupKey(name);
        }
    }
}
