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

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    public class CommandSetter
    {
        private readonly CommandGroupKey groupKey;

        public CommandGroupKey GroupKey { get { return this.groupKey; } }
        public CommandKey CommandKey { get; private set; }
        public CommandPropertiesSetter CommandPropertiesDefaults { get; private set; }

        public CommandSetter(CommandGroupKey groupKey)
        {
            this.groupKey = groupKey;
        }

        public static CommandSetter WithGroupKey(CommandGroupKey groupKey)
        {
            return new CommandSetter(groupKey);
        }
        public CommandSetter AndCommandKey(CommandKey commandKey)
        {
            CommandKey = commandKey;
            return this;
        }
        public CommandSetter AndCommandPropertiesDefaults(CommandPropertiesSetter commandPropertiesDefaults)
        {
            CommandPropertiesDefaults = commandPropertiesDefaults;
            return this;
        }
    }
}
