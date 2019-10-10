using GitHub.Services.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    [DataContract(IsReference = true)]
    public class WrappedException : ISecuredObject
    {
        public WrappedException()
        {
        }

        public WrappedException(Exception exception, Boolean includeErrorDetail, Version restApiVersion)
        {
            // if we have an AggregateException AND there is only one exception beneath it, let's use that
            // exception here instead.  for S2S calls, the exception coming in will be an Aggregate and will
            // loose type information if only the Aggregate is returned to the caller.
            if ((exception is AggregateException) &&
                (((AggregateException)exception).InnerExceptions != null) &&
                (((AggregateException)exception).Flatten().InnerExceptions.Count == 1))
            {
                // take the first one
                exception = ((AggregateException)exception).Flatten().InnerException;
            }

            // Populate the Type, TypeName, and TypeKey properties.
            Type type = exception.GetType();
            String typeName, typeKey;

            if (exception is VssServiceResponseException)
            {
                /*
                    * VssServiceResponseException takes an HttpStatusCode in its constructor which is 
                    * not compatible with the WrappableConstructors. Its not necessary to persist the 
                    * status code since it is bound to the response, so we just cast down to 
                    * VssServiceException to avoid conflict when unwrapping
                    */
                // It is okay for VssServiceResponseExceptions to happen on the server during S2S scenarios.
                // just do the translation -- don't Debug.Fail!
                //Debug.Fail("Do not throw VssServiceResponseException from the server side.");
                type = typeof(VssServiceException);
                VssException.GetTypeNameAndKeyForExceptionType(type, restApiVersion, out typeName, out typeKey);
            }
            else if (exception is VssServiceException)
            {
                ((VssServiceException)exception).GetTypeNameAndKey(restApiVersion, out typeName, out typeKey);
            }
            else
            {
                // Fall back to the base implementation
                VssException.GetTypeNameAndKeyForExceptionType(type, restApiVersion, out typeName, out typeKey);
            }

            this.Type = type;
            this.TypeName = typeName;
            this.TypeKey = typeKey;

            if (includeErrorDetail && exception.InnerException != null)
            {
                InnerException = new WrappedException(exception.InnerException, includeErrorDetail, restApiVersion);
            }

            Message = exception.Message;

            if (includeErrorDetail)
            {
                //if the exception was not thrown, it won't have a stack trace, so 
                //capture it here in that case. Skip last two frames, we don't want WrappedException
                //or its caller on the stack.
                StackTrace = exception.StackTrace ?? new StackTrace(2, true).ToString();
            }

            if (!string.IsNullOrWhiteSpace(exception.HelpLink))
            {
                HelpLink = exception.HelpLink;
            }

            if (exception is VssException)
            {
                EventId = ((VssException)exception).EventId;
                ErrorCode = ((VssException)exception).ErrorCode;
            }

            TryWrapCustomProperties(exception);
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public Dictionary<string, object> CustomProperties
        {
            get;
            set;
        }

        [DataMember]
        public WrappedException InnerException { get; set; }

        public Exception UnwrappedInnerException { get; set; }

        [DataMember]
        public String Message { get; set; }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String HelpLink { get; set; }

        public Type Type
        {
            get
            {
                if (m_type == null)
                {
                    //try to create the type from the TypeName
                    if (!String.IsNullOrEmpty(TypeName))
                    {
                        m_type = LoadType(TypeName);
                    }
                }

                return m_type;
            }

            set
            {
                m_type = value;
            }
        }

        private Type m_type;

        private string m_typeName;

        [DataMember]
        public String TypeName
        {
            get
            {
                return m_typeName;
            }

            set
            {
                if (value.Contains("Microsoft.VisualStudio"))
                {
                    m_typeName = value.Replace("Microsoft.VisualStudio", "GitHub");
                    m_typeName = m_typeName.Substring(0, m_typeName.IndexOf(",")) + ", Sdk";
                }
                else if (value.Contains("Microsoft.Azure.DevOps"))
                {
                    m_typeName = value.Replace("Microsoft.Azure.DevOps", "GitHub");
                    m_typeName = m_typeName.Substring(0, m_typeName.IndexOf(",")) + ", Sdk";
                }
                else if (value.Contains("Microsoft.TeamFoundation"))
                {
                    m_typeName = value.Replace("Microsoft.TeamFoundation", "GitHub");
                    m_typeName = m_typeName.Substring(0, m_typeName.IndexOf(",")) + ", Sdk";
                }
                else
                {
                    m_typeName = value;
                }
            }
        }

        [DataMember]
        public String TypeKey
        {
            get;
            set;
        }

        [DataMember]
        public int ErrorCode
        {
            get;
            set;
        }

        [DataMember]
        public int EventId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string StackTrace
        {
            get;
            set;
        }

        public Exception Unwrap(IDictionary<String, Type> typeMapping)
        {
            Exception innerException = null;
            if (InnerException != null)
            {
                innerException = InnerException.Unwrap(typeMapping);
                UnwrappedInnerException = innerException;
            }

            Exception exception = null;

            // if they have bothered to map type, use that first.
            if (!String.IsNullOrEmpty(TypeKey))
            {
                Type type;
                if (typeMapping != null && typeMapping.TryGetValue(TypeKey, out type) ||
                    baseTranslatedExceptions.TryGetValue(TypeKey, out type))
                {
                    try
                    {
                        this.Type = type;
                        exception = Activator.CreateInstance(this.Type, Message, innerException) as Exception;
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }

            if (exception == null)
            {
                //no standard mapping, fallback to 
                exception = UnWrap(innerException);
            }

            if (exception is VssException)
            {
                ((VssException)exception).EventId = this.EventId;
                ((VssException)exception).ErrorCode = this.ErrorCode;
            }

            if (exception == null && !String.IsNullOrEmpty(Message))
            {
                // NOTE: We can get exceptions that we can't create, IE. SqlException, AzureExceptions.
                // This is not a failure, we will just wrap the exception in a VssServiceException
                // since the type is not available.
                exception = new VssServiceException(Message, innerException);
            }

            if (exception == null && !string.IsNullOrEmpty(TypeName))
            {
                Debug.Assert(false, string.Format("Server exception cannot be resolved. Type name: {0}", TypeName));
            }

            if (exception != null
                && !string.IsNullOrEmpty(HelpLink))
            {
                exception.HelpLink = HelpLink;
            }

            if (exception != null
                && !string.IsNullOrEmpty(this.StackTrace))
            {
                FieldInfo stackTraceField = typeof(Exception).GetTypeInfo().GetDeclaredField("_stackTraceString");
                if (stackTraceField != null && !stackTraceField.Attributes.HasFlag(FieldAttributes.Public) && !stackTraceField.Attributes.HasFlag(FieldAttributes.Static))
                {
                    stackTraceField.SetValue(exception, this.StackTrace);
                }
            }

            if (exception != null && exception.GetType() == this.Type)
            {
                TryUnWrapCustomProperties(exception);
            }

            return exception;
        }

        private Exception UnWrap(Exception innerException)
        {
            Exception exception = null;
            if (this.Type != null)  // m_type is typically null when this.Type getter is hit from here, so the LoadType method will get invoked here.
            {
                try
                {
                    Object[] args = null;

                    ConstructorInfo info = GetMatchingConstructor(new[] { typeof(WrappedException) });
                    if (info != null)
                    {
                        // a constructor overload on an exception that takes a WrappedException, is useful
                        // in cases where the other constructors manipulate the string that we pass in,
                        // which we do not want to happen when unwrapping an exception.
                        args = new object[] { this };
                    }
                    else
                    {
                        info = GetMatchingConstructor(new[] { typeof(String), typeof(Exception) });
                        if (info != null)
                        {
                            args = new object[] { Message, innerException };
                        }
                        else
                        {
                            //try just string
                            info = GetMatchingConstructor(new[] { typeof(String) });
                            if (info != null)
                            {
                                args = new object[] { Message };
                            }
                            else
                            {
                                //try default constructor
                                info = GetMatchingConstructor(new Type[0]);
                            }
                        }
                    }
                    if (info != null)
                    {
                        exception = info.Invoke(args) as Exception;
                        // only check exceptions that derive from VssExceptions, since we don't have control
                        // to make code changes to exceptions that we don't own.
                        Debug.Assert(!(exception is VssException) || exception.Message == Message,
                            "The unwrapped exception message does not match the original exception message.",
                            "Type: {0}{1}Expected: {2}{1}Actual: {3}{1}{1}This can happen if the exception has a contructor that manipulates the input string.  You can work around this by creating a constructor that takes in a WrappedException which sets the message verbatim and optionally the inner exception.",
                            exception.GetType(),
                            Environment.NewLine,
                            Message,
                            exception.Message);
                    }
                }
                catch (Exception)
                { }
            }
            return exception;
        }

        private ConstructorInfo GetMatchingConstructor(params Type[] parameterTypes)
        {
            return this.Type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
        }

        private static Type LoadType(String typeName)
        {
            // For rest api version < 3.0, the server transmits the fulllAssemblyQualifiedName of exception at time that version was initially released,
            // which means normal loading will fail due to version mismatch, as the version will alwyas be <= 14.0.0.0.
            // Example: typeName=GitHub.Core.WebApi.ProjectDoesNotExistWithNameException, GitHub.Core.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a

            // For rest api version >= 3.0 (dev15), it just sends an assembly qualified type name without Version and PublicKeyToken, so it is version agnostic.
            // Example: typeName=GitHub.Core.WebApi.ProjectDoesNotExistWithNameException, GitHub.Core.WebApi

            // Order of precedence, 
            //  1. Standard .net type loading
            //  2. Check exception mapping attributes for compat scenarios with older severs
            //  3. If version 14 is specified in typeName, resolve assembly by switching to version 15
            //  4. Try and load, binary from same folder as this binary

            Type ret = null;

            //try normal loading first
            try
            {
                ret = Type.GetType(typeName, false, true);
            }
            catch (Exception)
            {
                // GetType can still throw an exception despite sending in false for throwOnError
            }

            if (ret == null)
            {
                // try and look up type mapping based on exception attributes.
                ret = LookupExceptionAttributeMapping(typeName);
                if (ret == null)
                {
                    try
                    {
                        //probably assembly version is wrong
                        //fortunately, .NET provides an overload for just such an eventuality
                        //without forcing us to parse the string
                        ret = Type.GetType(typeName,
                                            ResolveAssembly,
                                            null,
                                            false,
                                            true);
                    }
                    catch (Exception)
                    {
                        //we swallow all exceptions, some can potentially
                        //still occur, like BadImageFormat, even with throwOnError=false above
                    }
                }
            }
            return ret;
        }

        private static Assembly ResolveAssembly(AssemblyName asmName)
        {
            //if we get here we are probably in a back compat scenario
            //check the version of the asmName, and if it is 14.0, upgrade it to
            //the same as this assembly version and try it
            if (asmName.Version == null || asmName.Version.Major <= c_backCompatVer)
            {
                //create new instance, don't copy unknown params...
                AssemblyName newName = new AssemblyName
                {
                    Name = asmName.Name,
                    CultureInfo = asmName.CultureInfo
                };
                // DEVNOTE: Do not tack-on the version information, instead let the
                // assembly load without it so that it may resolve to the appropriate.
                // Otherwise, translation down the stack may fail due to version mismatch
                // and that end's up creating un-necessary retries on certain user defined exceptions.
                // newName.Version = Assembly.GetExecutingAssembly().GetName().Version;
                newName.SetPublicKeyToken(asmName.GetPublicKeyToken());

                try
                {
                    var ret = Assembly.Load(newName);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
                catch (Exception)
                { }
            }

            //Next,we look in the same directory, add .dll to the name and do a "LoadFrom" just
            //like in the AssemblyResolve event in other places
            //the assembly should be in the same directory as this one.
            string currentPath = Assembly.GetExecutingAssembly().Location;
            if (!String.IsNullOrEmpty(currentPath))
            {
                string fullPath = Path.Combine(Path.GetDirectoryName(currentPath), asmName.Name + ".dll");

                if (File.Exists(fullPath))
                {
                    return Assembly.LoadFrom(fullPath);
                }
            }
            return null;
        }

        private const int c_backCompatVer = 14;

        private static Type LookupExceptionAttributeMapping(string typeName)
        {
            Type mappedType = null;
            Tuple<Version, Type> cacheEntry = null;
            lock (syncObject)
            {
                if (!s_exceptionsWithAttributeMapping.TryGetValue(typeName, out cacheEntry))
                {
                    // if not in the cache, then we should update the cache and try again
                    UpdateExceptionAttributeMappingCache();
                    s_exceptionsWithAttributeMapping.TryGetValue(typeName, out cacheEntry);
                }
            }
            if (cacheEntry != null)
            {
                mappedType = cacheEntry.Item2;
            }
            return mappedType;
        }

        /// <summary>
        /// Loop through all types in all loaded assemblies that we haven't looked at yet, and cache ExceptionMappingAttribute data
        /// </summary>
        private static void UpdateExceptionAttributeMappingCache()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !s_assembliesCheckedForExceptionMappings.Contains(a)))
            {
                if (DoesAssemblyQualify(assembly)) // only look at assemblies that match this binary's major version and public key token
                {
                    try
                    {

                        IEnumerable<Type> types;
                        try
                        {
                            // calling GetTypes has side effect of loading direct dependancies of the assembly.
                            types = assembly.GetTypes();
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            // if dependant assembly fails to load, we should still be able to get all the exceptions, since it would be unlikely,
                            // that an exception is referencing a type from the assembly that failed to load.
                            types = ex.Types.Where<Type>(t => t != null);
                        }

                        foreach (TypeInfo typeInfo in types)
                        {
                            foreach (ExceptionMappingAttribute attribute in typeInfo.GetCustomAttributes<ExceptionMappingAttribute>())
                            {
                                Tuple<Version, Type> cachedValue;

                                // Check if the TypeName already exists in cache and add it if not.  if it does exist, update if it has a higher ExclusiveMaxApiVersion.
                                // (In theory an old exception could be mapped to more then one type in the case we want the latest server
                                // to send different older types to different versions of older clients.  This method is used only on client when converting a type 
                                // from an older server, so we want the latest mapping of the older type.)
                                if (!s_exceptionsWithAttributeMapping.TryGetValue(attribute.TypeName, out cachedValue) || attribute.ExclusiveMaxApiVersion > cachedValue.Item1)
                                {
                                    s_exceptionsWithAttributeMapping[attribute.TypeName] = new Tuple<Version, Type>(attribute.ExclusiveMaxApiVersion, typeInfo.AsType());
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // if for any reason we can't get the defined types, we don't want an exception here to mask the real exception.
                    }
                }
                s_assembliesCheckedForExceptionMappings.Add(assembly); // keep track of all assemblies we have either ruled out or cached mappings for, so we don't have to consider them again
            }
        }

        /// <summary>
        /// Checks Assembly to see if it has the possibility to contain an ExceptionMappingAttribute.  Does this by matching the Version and PublicKeyToken
        /// with the current executing assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static bool DoesAssemblyQualify(Assembly assembly)
        {
            if (s_currentAssemblyPublicKeyToken == null || s_currentAssemblyVersion == null)
            {
                // cache these so we don't have to recompute every time we check an assembly
                AssemblyName thisAssemblyName = typeof(WrappedException).GetTypeInfo().Assembly.GetName();
                s_currentAssemblyPublicKeyToken = thisAssemblyName.GetPublicKeyToken();
                s_currentAssemblyVersion = thisAssemblyName.Version;
            }
            AssemblyName assemblyName = assembly.GetName();
            if (assemblyName.Version.Major != s_currentAssemblyVersion.Major)
            {
                return false;
            }
            byte[] assemblyPublicKeyToken = assemblyName.GetPublicKeyToken();

            // Allow the test code public key token as well, because we have an L0 test which declares an exception
            // that has ExceptionMappingAttribute.
            return ArrayUtility.Equals(s_currentAssemblyPublicKeyToken, assemblyPublicKeyToken) ||
                   ArrayUtility.Equals(s_testCodePublicKeyToken, assemblyPublicKeyToken);
        }

        private static object syncObject = new Object();
        private static byte[] s_currentAssemblyPublicKeyToken = null;
        private static Version s_currentAssemblyVersion = null;
        private static HashSet<Assembly> s_assembliesCheckedForExceptionMappings = new HashSet<Assembly>();
        private static readonly byte[] s_testCodePublicKeyToken = new byte[] { 0x68, 0x9d, 0x5c, 0x3b, 0x19, 0xaa, 0xe6, 0x23 };

        /// <summary>
        /// Exception Attribute Mapping Cache. key = exception type name from a response, value = ExclusiveMaxApiVersion and the mapped Type for that type name
        /// </summary>
        private static Dictionary<string, Tuple<Version, Type>> s_exceptionsWithAttributeMapping = new Dictionary<string, Tuple<Version, Type>>();

        private void TryWrapCustomProperties(Exception exception)
        {
            var customPropertiesWithDataMemberAttribute = GetCustomPropertiesInfo();

            if (customPropertiesWithDataMemberAttribute.Any())
            {
                this.CustomProperties = new Dictionary<string, object>();
            }

            foreach (var customProperty in customPropertiesWithDataMemberAttribute)
            {
                try
                {
                    this.CustomProperties.Add(customProperty.Name, customProperty.GetValue(exception));
                }
                catch
                {
                    // skip this property
                }
            }
        }

        private void TryUnWrapCustomProperties(Exception exception)
        {
            if (this.CustomProperties != null)
            {
                foreach (var property in GetCustomPropertiesInfo())
                {
                    if (this.CustomProperties.ContainsKey(property.Name))
                    {
                        try
                        {
                            var propertyValue = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(CustomProperties[property.Name]), property.PropertyType);
                            property.SetValue(exception, propertyValue);
                        }
                        catch
                        {
                            // skip this property
                        }
                    }
                }
            }
        }

        private IEnumerable<PropertyInfo> GetCustomPropertiesInfo()
        {
            return this.Type.GetTypeInfo().DeclaredProperties.Where(p => p.GetMethod.Attributes.HasFlag(MethodAttributes.Public)
                && !p.GetMethod.Attributes.HasFlag(MethodAttributes.Static)
                && p.CustomAttributes.Any(a => a.AttributeType.GetTypeInfo().IsAssignableFrom(typeof(DataMemberAttribute).GetTypeInfo())));
        }


        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => throw new NotImplementedException();

        int ISecuredObject.RequiredPermissions => throw new NotImplementedException();

        string ISecuredObject.GetToken() => throw new NotImplementedException();
        #endregion  

        // Exception translation rules which apply to all VssHttpClientBase subclasses
        private static readonly IDictionary<String, Type> baseTranslatedExceptions = new Dictionary<string, Type>()
        {
            { "VssAccessCheckException", typeof(Security.AccessCheckException) }
        };
    }
}
