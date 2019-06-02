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
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    public class EventNotifierDefault : IEventNotifier
    {
        private static EventNotifierDefault instance = new EventNotifierDefault();
        public static EventNotifierDefault Instance { get { return instance; } }
        protected EventNotifierDefault()
        {
        }

        public virtual void MarkEvent(CommandGroupKey group, CommandKey key, EventType eventType)
        {
            // do nothing
        }
        public virtual void MarkCommandExecution(CommandGroupKey group, CommandKey key, long elapsedTimeInMilliseconds)
        {
            // do nothing
        }
        public virtual void MarkExecutionConcurrency(CommandGroupKey group, CommandKey key, long executionSemaphoreNumberOfPermitsUsed)
        {
            // do nothing
        }
        public virtual void MarkFallbackConcurrency(CommandGroupKey group, CommandKey key, long fallbackSemaphoreNumberOfPermitsUsed)
        {
            // do nothing
        }
        public void MarkExecutionCount(CommandGroupKey group, CommandKey key, long executionCount)
        {
            // do nothing
        }
        public void MarkFallbackCount(CommandGroupKey group, CommandKey key, long fallbackCount)
        {
            // do nothing
        }
        public virtual void TraceRaw(int tracepoint, TraceLevel level, string featurearea, string classname, string message)
        {
            // do nothing
        }
    }
}
