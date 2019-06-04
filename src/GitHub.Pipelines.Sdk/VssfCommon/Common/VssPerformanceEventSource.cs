using System;
using System.Diagnostics.Tracing;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Note: This is our perfview event source which is used for performance troubleshooting
    /// Sadly, EventSource has few overloads so anything that isn't in http://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource.writeevent.aspx
    /// will cause a bunch of allocations - so we use manual interop for anything non trivial.
    /// 
    /// </summary>
    public sealed class VssPerformanceEventSource : EventSource
    {
        public static VssPerformanceEventSource Log = new VssPerformanceEventSource();

        #region WriteEvent PInvoke Overrides
        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u1, Guid u2, string st)
        {
            if (IsEnabled())
            {
                st = st ?? String.Empty;
                const int parameters = 3;
                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].Size = sizeof(Guid);
                dataDesc[0].DataPointer = (IntPtr)(&u1);
                dataDesc[1].Size = sizeof(Guid);
                dataDesc[1].DataPointer = (IntPtr)(&u2);
                dataDesc[2].Size = (st.Length + 1) * sizeof(char);

                fixed (char* pcst = st)
                {
                    dataDesc[2].DataPointer = (IntPtr)pcst;
                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u1, Guid u2, string st, long duration)
        {
            if (IsEnabled())
            {
                st = st ?? String.Empty;
                const int parameters = 4;
                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].Size = sizeof(Guid);
                dataDesc[0].DataPointer = (IntPtr)(&u1);
                dataDesc[1].Size = sizeof(Guid);
                dataDesc[1].DataPointer = (IntPtr)(&u2);
                dataDesc[2].Size = (st.Length + 1) * sizeof(char);
                dataDesc[3].Size = sizeof(long);
                dataDesc[3].DataPointer = (IntPtr)(&duration);

                fixed (char* pcst = st)
                {
                    dataDesc[2].DataPointer = (IntPtr)pcst;

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u, string st)
        {
            if (IsEnabled())
            {
                st = st ?? String.Empty;
                const int parameters = 2;
                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].DataPointer = (IntPtr)(&u);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].Size = (st.Length + 1) * sizeof(char);

                fixed (char* pcSt = st)
                {
                    dataDesc[1].DataPointer = (IntPtr)(pcSt);
                
                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u, string st, long duration)
        {
            if (IsEnabled())
            {
                st = st ?? String.Empty;
                const int parameters = 3;
                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].DataPointer = (IntPtr)(&u);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].Size = (st.Length + 1) * sizeof(char);
                dataDesc[2].Size = sizeof(long);
                dataDesc[2].DataPointer = (IntPtr)(&duration);

                fixed (char* pcSt = st)
                {
                    dataDesc[1].DataPointer = (IntPtr)(pcSt);

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u)
        {
            if (IsEnabled())
            {
                EventData dataDesc = new EventData(); // this is a struct so no allocation here

                dataDesc.DataPointer = (IntPtr)(&u);
                dataDesc.Size = sizeof(Guid);
                WriteEventCore(eventId, 1, &dataDesc);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u, long duration)
        {
            if (IsEnabled())
            {
                const int parameters = 2;
                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].DataPointer = (IntPtr)(&u);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].DataPointer = (IntPtr)(&duration);
                dataDesc[1].Size = sizeof(long);

                WriteEventCore(eventId, parameters, dataDesc);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u1, string st1, DateTime dt1, DateTime dt2, Guid u2) // Guid uniqueIdentifier, string name, string validFrom, string validTo, Guid contextId
        {
            if (IsEnabled())
            {
                st1 = st1 ?? String.Empty;
                long ft1 = dt1.ToFileTimeUtc();
                long ft2 = dt2.ToFileTimeUtc();

                const int parameters = 5;

                EventData* dataDesc = stackalloc EventData[parameters];
                dataDesc[0].DataPointer = (IntPtr)(&u1);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].Size = (st1.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&ft1);
                dataDesc[2].Size = sizeof(long);
                dataDesc[3].DataPointer = (IntPtr)(&ft2);
                dataDesc[3].Size = sizeof(long);
                dataDesc[4].DataPointer = (IntPtr)(&u2);
                dataDesc[4].Size = sizeof(Guid);

                fixed (char* pcst1 = st1)
                {
                    dataDesc[1].DataPointer = (IntPtr)(pcst1);

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid u1, string st1, string st2, string st3, Guid u2, long duration) // Guid uniqueIdentifier, string name, string validFrom, string validTo, Guid contextId
        {
            if (IsEnabled())
            {
                st1 = st1 ?? String.Empty;
                st2 = st2 ?? String.Empty;
                st3 = st3 ?? String.Empty;

                const int parameters = 6;

                EventData* dataDesc = stackalloc EventData[parameters];
                dataDesc[0].DataPointer = (IntPtr)(&u1);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].Size = (st1.Length + 1) * sizeof(char);
                dataDesc[2].Size = (st2.Length + 1) * sizeof(char);
                dataDesc[3].Size = (st3.Length + 1) * sizeof(char);
                dataDesc[4].DataPointer = (IntPtr)(&u2);
                dataDesc[4].Size = sizeof(Guid);
                dataDesc[5].DataPointer = (IntPtr)(&duration);
                dataDesc[5].Size = sizeof(long);

                fixed (char* pcst1 = st1, pcst2 = st2, pcst3 = st3)
                {
                    dataDesc[1].DataPointer = (IntPtr)(pcst1);
                    dataDesc[2].DataPointer = (IntPtr)(pcst2);
                    dataDesc[3].DataPointer = (IntPtr)(pcst3);

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid uniqueIdentifier, string st1, string st2, string st3)
        {
            if (IsEnabled())
            {
                st1 = st1 ?? String.Empty;
                st2 = st2 ?? String.Empty;
                st3 = st3 ?? String.Empty;

                const int parameters = 4;
                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].DataPointer = (IntPtr)(&uniqueIdentifier);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].Size = (st1.Length + 1) * sizeof(char);
                dataDesc[2].Size = (st2.Length + 1) * sizeof(char);
                dataDesc[3].Size = (st3.Length + 1) * sizeof(char);

                fixed (char* pcst1 = st1, pcst2 = st2, pcst3 = st3)
                {
                    dataDesc[1].DataPointer = (IntPtr)(pcst1);
                    dataDesc[2].DataPointer = (IntPtr)(pcst2);   
                    dataDesc[3].DataPointer = (IntPtr)(pcst3);

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid uniqueIdentifier, string st1, string st2, string st3, long duration)
        {
            if (IsEnabled())
            {
                st1 = st1 ?? String.Empty;
                st2 = st2 ?? String.Empty;
                st3 = st3 ?? String.Empty;

                const int parameters = 5;

                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].DataPointer = (IntPtr)(&uniqueIdentifier);
                dataDesc[0].Size = sizeof(Guid);

                dataDesc[1].Size = (st1.Length + 1) * sizeof(char);
                dataDesc[2].Size = (st2.Length + 1) * sizeof(char);
                dataDesc[3].Size = (st3.Length + 1) * sizeof(char);

                dataDesc[4].DataPointer = (IntPtr)(&duration);
                dataDesc[4].Size = sizeof(long);

                fixed (char* pcst1 = st1, pcst2 = st2, pcst3 = st3)
                {
                    dataDesc[1].DataPointer = (IntPtr)(pcst1);
                    dataDesc[2].DataPointer = (IntPtr)(pcst2);
                    dataDesc[3].DataPointer = (IntPtr)(pcst3);

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid uniqueIdentifier, string st1, string st2)
        {
            if (IsEnabled())
            {
                st1 = st1 ?? String.Empty;
                st2 = st2 ?? String.Empty;

                const int parameters = 3;
                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].DataPointer = (IntPtr)(&uniqueIdentifier);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].Size = (st1.Length + 1) * sizeof(char);
                dataDesc[2].Size = (st2.Length + 1) * sizeof(char);

                fixed (char* pcst1 = st1, pcst2 = st2)
                {
                    dataDesc[1].DataPointer = (IntPtr)(pcst1);
                    dataDesc[2].DataPointer = (IntPtr)(pcst2);

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, Guid uniqueIdentifier, string st1, string st2, long duration)
        {
            if (IsEnabled())
            {
                st1 = st1 ?? String.Empty;
                st2 = st2 ?? String.Empty;

                const int parameters = 4;

                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].DataPointer = (IntPtr)(&uniqueIdentifier);
                dataDesc[0].Size = sizeof(Guid);
                dataDesc[1].Size = (st1.Length + 1) * sizeof(char);
                dataDesc[2].Size = (st2.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&duration);
                dataDesc[3].Size = sizeof(long);

                fixed (char* pcst1 = st1, pcst2 = st2)
                {    
                    dataDesc[1].DataPointer = (IntPtr)(pcst1);
                    dataDesc[2].DataPointer = (IntPtr)(pcst2);
 
                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string st, int i1, long duration)
        {
            if (IsEnabled())
            {
                const int parameters = 3;
                st = st ?? String.Empty;

                EventData* dataDesc = stackalloc EventData[parameters];

                dataDesc[0].Size = (st.Length + 1) * sizeof(char);
                dataDesc[1].DataPointer = (IntPtr)(&i1);
                dataDesc[1].Size = sizeof(Int32);
                dataDesc[2].DataPointer = (IntPtr)(&duration);
                dataDesc[2].Size = sizeof(int);

                fixed (char* pcst = st)
                {
                    dataDesc[0].DataPointer = (IntPtr)(pcst);

                    WriteEventCore(eventId, parameters, dataDesc);
                }
            }
        }
        #endregion

        public void MethodStart(Guid uniqueIdentifier, Guid hostId, string methodName)
        {
            WriteEvent(1, uniqueIdentifier, hostId, methodName);
        }

        public void MethodStop(Guid uniqueIdentifier, Guid hostId, string methodName, long duration)
        {
            WriteEvent(2, uniqueIdentifier, hostId, methodName, duration);
        }

        public void NotificationCallbackStart(Guid hostId, string callback)
        {
            WriteEvent(3, hostId, callback);
        }

        public void NotificationCallbackStop(Guid hostId, string callback, long duration)
        {
            WriteEvent(4, hostId, callback, duration);
        }

        public void TaskCallbackStart(Guid hostId, string callback)
        {
            WriteEvent(5, hostId, callback);
        }

        public void TaskCallbackStop(Guid hostId, string callback, long duration)
        {
            WriteEvent(6, hostId, callback, duration);
        }

        public void StopHostTaskStart(Guid hostId)
        {
            WriteEvent(7, hostId);
        }

        public void StopHostTaskStop(Guid hostId, long duration)
        {
            WriteEvent(8, hostId, duration);
        }

        public void RefreshSecurityTokenStart(Guid uniqueIdentifier, string name)
        {
            WriteEvent(9, uniqueIdentifier, name);
        }

        public void RefreshSecurityTokenStop(Guid uniqueIdentifier, string name, DateTime validFrom, DateTime validTo, Guid contextId, long duration)
        {
            WriteEvent(10, uniqueIdentifier, name, validFrom, validTo, contextId, duration);
        }

        public void SQLStart(Guid uniqueIdentifier, string query, string server, string databaseName)
        {
            WriteEvent(11, uniqueIdentifier, query, server, databaseName);
        }

        public void SQLStop(Guid uniqueIdentifier, string query, string server, string databaseName, long duration)
        {
            WriteEvent(12, uniqueIdentifier, query, server, databaseName, duration);
        }

        public void RESTStart(Guid uniqueIdentifier, string message)
        {
            WriteEvent(13, uniqueIdentifier, message);
        }

        public void RESTStop(Guid uniqueIdentifier, Guid originalActivityId, string message, long duration)
        {
            WriteEvent(14, uniqueIdentifier, originalActivityId, message, duration);
        }

        public void WindowsAzureStorageStart(Guid uniqueIdentifier, string accountName, string methodName)
        {
            WriteEvent(15, uniqueIdentifier, accountName, methodName);
        }

        public void WindowsAzureStorageStop(Guid uniqueIdentifier, string accountName, string methodName, long duration)
        {
            WriteEvent(16, uniqueIdentifier, accountName, methodName, duration);
        }

        public void LoadHostStart(Guid hostId)
        {
            WriteEvent(17, hostId);
        }

        public void LoadHostStop(Guid hostId, long duration)
        {
            WriteEvent(18, hostId, duration);
        }

        /// <summary>
        /// This method is intentionally called Begin, not Start(), since it's a recursive event
        /// Service Profiler cannot deal with recursive events unless you have the
        /// [Event(EventActivityOptions.Recursive)] however that is not supported in 4.5 currently
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <param name="hostId"></param>
        /// <param name="serviceType"></param>
        public void CreateServiceInstanceBegin(Guid uniqueIdentifier, Guid hostId, string serviceType)
        {
            WriteEvent(19, uniqueIdentifier, hostId, serviceType);
        }

        /// <summary>
        /// This method is intentionally called Begin, not Start(), since it's a recursive event
        /// Service Profiler cannot deal with recursive events unless you have the
        /// [Event(EventActivityOptions.Recursive)] however that is not supported in 4.5 currently
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <param name="hostId"></param>
        /// <param name="serviceType"></param>
        /// <param name="duration"></param>
        public void CreateServiceInstanceEnd(Guid uniqueIdentifier, Guid hostId, string serviceType, long duration)
        {
            WriteEvent(20, uniqueIdentifier, hostId, serviceType, duration);
        }

        public void DetectedLockReentryViolation(string lockName)
        {
            WriteEvent(21, lockName);
        }

        public void DetectedLockUsageViolation(string lockName, string locksHeld)
        {
            WriteEvent(22, lockName, locksHeld);
        }

        public void RedisStart(Guid uniqueIdentifier, string operation, string ciArea, string cacheArea)
        {
            WriteEvent(23, uniqueIdentifier, operation, ciArea, cacheArea);
        }

        public void RedisStop(Guid uniqueIdentifier, string operation, string ciArea, string cacheArea, long duration)
        {
            WriteEvent(24, uniqueIdentifier, operation, ciArea, cacheArea, duration);
        }

        public void MessageBusSendBatchStart(Guid uniqueIdentifier, string messageBusName, int numberOfMessages)
        {
            WriteEvent(25, uniqueIdentifier, messageBusName, numberOfMessages);
        }

        public void MessageBusSendBatchStop(Guid uniqueIdentifier, string messageBusName, int numberOfMessages, long duration)
        {
            WriteEvent(26, uniqueIdentifier, messageBusName, numberOfMessages, duration);
        }

    }
}
