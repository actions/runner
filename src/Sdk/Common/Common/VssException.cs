// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Base class for all custom exceptions thrown from Vss and Tfs code. 
    /// </summary>
    /// <remarks>
    /// All Exceptions in the VSS space -- any exception that flows across
    /// a REST API boudary -- should derive from VssServiceException. This is likely
    /// almost ALL new exceptions. Legacy TFS exceptions that do not flow through rest
    /// derive from TeamFoundationServerException or TeamFoundationServiceException
    /// </remarks>
    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [ExceptionMapping("0.0", "3.0", "VssException", "GitHub.Services.Common.VssException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public abstract class VssException : ApplicationException
    {
        /// <summary>
        /// No-arg constructor that sumply defers to the base class.
        /// </summary>
        public VssException() : base()
        {
        }

        /// <summary>
        /// Initializes an exception with the specified error message.
        /// </summary>
        /// <param name="errorCode">Application-defined error code for this exception</param>
        public VssException(int errorCode) : this(errorCode, false)
        {
        }

        /// <summary>
        /// Initializes an exception with the specified error message.
        /// </summary>
        /// <param name="errorCode">Application-defined error code for this exception</param>
        /// <param name="logException">Indicate whether this exception should be logged</param>
        public VssException(int errorCode, bool logException)
        {
            ErrorCode = errorCode;
            LogException = logException;
        }

        /// <summary>
        /// Initializes an exception with the specified error message.
        /// </summary>
        /// <param name="message">A human readable message that describes the error</param>
        public VssException(string message) : base(SecretUtility.ScrubSecrets(message))
        {
        }

        /// <summary>
        /// Initializes an exception with the specified error message and an inner exception that caused this exception to be raised.
        /// </summary>
        /// <param name="message">A human readable message that describes the error</param>
        /// <param name="innerException"></param>
        public VssException(string message, Exception innerException) : base(SecretUtility.ScrubSecrets(message), innerException)
        {
        }

        /// <summary>
        /// Initializes an exception with the specified error message and an inner exception that caused this exception to be raised.
        /// </summary>
        /// <param name="message">A human readable message that describes the error</param>
        /// <param name="errorCode">Application defined error code</param>
        /// <param name="innerException"></param>
        public VssException(string message, int errorCode, Exception innerException) : this(message, innerException)
        {
            ErrorCode = errorCode;
            LogException = false;
        }

        /// <summary>
        /// Initializes an exception with the specified error message and an inner exception that caused this exception to be raised.
        /// </summary>
        /// <param name="message">A human readable message that describes the error</param>
        /// <param name="errorCode">Application defined error code</param>
        public VssException(string message, int errorCode) : this(message, errorCode, false)
        {
        }

        /// <summary>
        /// Initializes an exception with the specified error message and an inner exception that caused this exception to be raised.
        /// </summary>
        /// <param name="message">A human readable message that describes the error</param>
        /// <param name="errorCode">Application defined error code</param>
        /// <param name="logException">Indicate whether this exception should be logged</param>
        public VssException(string message, int errorCode, bool logException) : this(message)
        {
            ErrorCode = errorCode;
            LogException = logException;
        }

        /// <summary>
        /// Initializes an exception with the specified error message and an inner exception that caused this exception to be raised.
        /// </summary>
        /// <param name="message">A human readable message that describes the error</param>
        /// <param name="errorCode">Application defined error code</param>
        /// <param name="logException"></param>
        /// <param name="innerException"></param>
        public VssException(string message, int errorCode, bool logException, Exception innerException) : this(message, innerException)
        {
            ErrorCode = errorCode;
            LogException = logException;
        }

        /// <summary>
        /// Initializes an exception from serialized data
        /// </summary>
        /// <param name="info">object holding the serialized data</param>
        /// <param name="context">context info about the source or destination</param>
        protected VssException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            LogException = (bool)info.GetValue("m_logException", typeof(bool));
            ReportException = (bool)info.GetValue("m_reportException", typeof(bool));
            ErrorCode = (int)info.GetValue("m_errorCode", typeof(int));
            EventId = (int)info.GetValue("m_eventId", typeof(int));
        }

        [SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("m_logException", LogException);
            info.AddValue("m_reportException", ReportException);
            info.AddValue("m_errorCode", ErrorCode);
            info.AddValue("m_eventId", EventId);
        }

            /// <summary>Indicate whether this exception instance should be logged</summary>
            /// <value>True (false) if the exception should (should not) be logged</value>
        public bool LogException
        {
            get
            {
                return m_logException;
            }
            set
            {
                m_logException = value;
            }
        }

        /// <summary>A user-defined error code.</summary>
        public int ErrorCode
        {
            get
            {
                return m_errorCode;
            }
            set
            {
                m_errorCode = value;
            }
        }

        /// <summary>The event ID to report if the exception is marked for the event log</summary>
        /// <value>The event ID used in the entry added to the event log</value>
        public int EventId
        {
            get
            {
                return m_eventId;
            }
            set
            {
                m_eventId = value;
            }
        }

        /// <summary>Indicate whether the exception should be reported through Dr. Watson</summary>
        /// <value>True (false) if the exception should (should not) be reported</value>
        public bool ReportException
        {
            get
            {
                return m_reportException;
            }
            set
            {
                m_reportException = value;
            }
        }

        /// <summary>
        /// Gets the default serialized type name and type key for the given exception type.
        /// </summary>
        internal static void GetTypeNameAndKeyForExceptionType(Type exceptionType, Version restApiVersion, out String typeName, out String typeKey)
        {
            typeName = null;
            typeKey = exceptionType.Name;
            if (restApiVersion != null)
            {
                IEnumerable<ExceptionMappingAttribute> exceptionAttributes = exceptionType.GetTypeInfo().GetCustomAttributes<ExceptionMappingAttribute>().Where(ea => ea.MinApiVersion <= restApiVersion && ea.ExclusiveMaxApiVersion > restApiVersion);
                if (exceptionAttributes.Any())
                {
                    ExceptionMappingAttribute exceptionAttribute = exceptionAttributes.First();
                    typeName = exceptionAttribute.TypeName;
                    typeKey = exceptionAttribute.TypeKey;
                }
                else if (restApiVersion < s_backCompatExclusiveMaxVersion)  //if restApiVersion < 3 we send the assembly qualified name with the current binary version switched out to 14
                {
                    typeName = GetBackCompatAssemblyQualifiedName(exceptionType);
                }
            }
            
            if (typeName == null)
            {

                AssemblyName asmName = exceptionType.GetTypeInfo().Assembly.GetName();
                if (asmName != null)
                {
                    //going forward we send "FullName" and simple assembly name which includes no version.
                    typeName = exceptionType.FullName + ", " + asmName.Name;
                }
                else
                {
                    String assemblyString = exceptionType.GetTypeInfo().Assembly.FullName;
                    assemblyString = assemblyString.Substring(0, assemblyString.IndexOf(','));
                    typeName = exceptionType.FullName + ", " + assemblyString;
                }
            }
        }

        internal static String GetBackCompatAssemblyQualifiedName(Type type)
        {
            AssemblyName current = type.GetTypeInfo().Assembly.GetName();
            if (current != null)
            {
                AssemblyName old = current;
                old.Version = new Version(c_backCompatVersion, 0, 0, 0);
                return Assembly.CreateQualifiedName(old.ToString(), type.FullName);
            }
            else
            {
                //this is probably not necessary...
                return type.AssemblyQualifiedName.Replace(c_currentAssemblyMajorVersionString, c_backCompatVersionString);
            }
        }

        private const String c_currentAssemblyMajorVersionString = "Version=" + GeneratedVersionInfo.AssemblyMajorVersion;
        private const String c_backCompatVersionString = "Version=14";
        private const int c_backCompatVersion = 14;

        private static Version s_backCompatExclusiveMaxVersion = new Version(3, 0);
        private bool m_logException;
        private bool m_reportException;
        private int m_errorCode;

        private int m_eventId = DefaultExceptionEventId;

        //From EventLog.cs in Framework.
        public const int DefaultExceptionEventId = 3000;
    }
}
