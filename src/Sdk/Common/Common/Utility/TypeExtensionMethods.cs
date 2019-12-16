using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace GitHub.Services.Common
{
    public static class TypeExtensionMethods
    {
        /// <summary>
        /// Determins if a value is assignable to the requested type.  It goes
        /// the extra step beyond IsAssignableFrom in that it also checks for
        /// IConvertible and attempts to convert the value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsAssignableOrConvertibleFrom(this Type type, object value)
        {
            if (value == null)
            {
                return false;
            }

            if (!type.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
            {
                if (value is IConvertible)
                {
                    // Try and convert to the requested type, if successful
                    // assign value to the result so we don't have to do again.
                    try
                    {
                        ConvertUtility.ChangeType(value, type, CultureInfo.CurrentCulture);
                        return true;
                    }
                    catch (FormatException)
                    {
                    }
                    catch (InvalidCastException)
                    {
                    }
                    catch (OverflowException)
                    {
                    }
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the type is of the type t.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="t">The type to compare to.</param>
        /// <returns>True if of the same type, otherwise false.</returns>
        public static bool IsOfType(this Type type, Type t)
        {
            if (t.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                return true;
            }
            else if (type.GetTypeInfo().IsGenericType &&
                     type.GetGenericTypeDefinition() == t)
            {
                //generic type
                return true;
            }
            else if (type.GetTypeInfo().ImplementedInterfaces.Any(
                        i => i.GetTypeInfo().IsGenericType &&
                             i.GetGenericTypeDefinition() == t))
            {
                //implements generic type
                return true;
            }

            return false;
        }


        /// <summary>
        /// Determines if the type is a Dictionary.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if a dictionary, otherwise false.</returns>
        public static bool IsDictionary(this Type type)
        {
            if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                //non-generic dictionary
                return true;
            }
            else if (type.GetTypeInfo().IsGenericType &&
                     type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                //generic dictionary interface
                return true;
            }
            else if (type.GetTypeInfo().ImplementedInterfaces.Any(
                        i => i.GetTypeInfo().IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                //implements generic dictionary
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the type is a List.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if a list, otherwise false.</returns>
        public static bool IsList(this Type type)
        {
            if (typeof(IList).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                //non-generic list
                return true;
            }
            else if (type.GetTypeInfo().IsGenericType &&
                     type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                //generic list interface
                return true;
            }
            else if (type.GetTypeInfo().ImplementedInterfaces.Any(
                        i => i.GetTypeInfo().IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                //implements generic list
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get's the type of the field/property specified.  
        /// </summary>
        /// <param name="type">The type to get the field/property from.</param>
        /// <param name="name">The name of the field/property.</param>
        /// <returns>The type of the field/property or null if no match found.</returns>
        public static Type GetMemberType(this Type type, string name)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            PropertyInfo propertyInfo = GetPublicInstancePropertyInfo(type, name);
            if (propertyInfo != null)
            {
                return propertyInfo.PropertyType;
            }
            else
            {
                FieldInfo fieldInfo = GetPublicInstanceFieldInfo(type, name);
                if (fieldInfo != null)
                {
                    return fieldInfo.FieldType;
                }
            }
            return null;
        }

        /// <summary>
        /// Get's the value of the field/property specified.  
        /// </summary>
        /// <param name="type">The type to get the field/property from.</param>
        /// <param name="name">The name of the field/property.</param>
        /// <param name="obj">The object to get the value from.</param>
        /// <returns>The value of the field/property or null if no match found.</returns>
        public static object GetMemberValue(this Type type, string name, object obj)
        {
            PropertyInfo propertyInfo = GetPublicInstancePropertyInfo(type, name);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(obj);
            }
            else
            {
                FieldInfo fieldInfo = GetPublicInstanceFieldInfo(type, name);
                if (fieldInfo != null)
                {
                    return fieldInfo.GetValue(obj);
                }
            }
            return null;
        }

        /// <summary>
        /// Set's the value of the field/property specified.  
        /// </summary>
        /// <param name="type">The type to get the field/property from.</param>
        /// <param name="name">The name of the field/property.</param>
        /// <param name="obj">The object to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public static void SetMemberValue(this Type type, string name, object obj, object value)
        {
            PropertyInfo propertyInfo = GetPublicInstancePropertyInfo(type, name);
            if (propertyInfo != null)
            {
                if (!propertyInfo.SetMethod.IsPublic)
                {
                    // this is here to match original behaviour before we switched to PCL version of code.
                    throw new ArgumentException("Property set method not public.");
                }
                propertyInfo.SetValue(obj, value);
            }
            else
            {
                FieldInfo fieldInfo = GetPublicInstanceFieldInfo(type, name);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(obj, value);
                }
            }
        }

        /// <summary>
        /// Portable compliant way to get a constructor with specified arguments.  This will return a constructor that is public or private as long as the arguments match.  NULL will be returned if there is no match.
        /// Note that it will pick the first one it finds that matches, which is not necesarily the best match.
        /// </summary>
        /// <param name="type">The Type that has the constructor</param>
        /// <param name="parameterTypes">The type of the arguments for the constructor.</param>
        /// <returns></returns>
        public static ConstructorInfo GetFirstMatchingConstructor(this Type type, params Type[] parameterTypes)
        {
            return type.GetTypeInfo().DeclaredConstructors.GetFirstMatchingConstructor(parameterTypes);
        }

        /// <summary>
        /// Portable compliant way to get a constructor with specified arguments from a prefiltered list.  This will return a constructor that is public or private as long as the arguments match.  NULL will be returned if there is no match.
        /// Note that it will pick the first one it finds that matches, which is not necesarily the best match.
        /// </summary>
        /// <param name="constructors">Prefiltered list of constructors</param>
        /// <param name="parameterTypes">The type of the arguments for the constructor.</param>
        /// <returns></returns>
        public static ConstructorInfo GetFirstMatchingConstructor(this IEnumerable<ConstructorInfo> constructors, params Type[] parameterTypes)
        {
            foreach (ConstructorInfo constructorInfo in constructors)
            {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if (parameters.Length == parameterTypes.Length)
                {
                    int i;
                    bool matches = true;
                    for (i = 0; i < parameterTypes.Length; i++)
                    {
                        if (parameters[i].ParameterType != parameterTypes[i] && !parameters[i].ParameterType.GetTypeInfo().IsAssignableFrom(parameterTypes[i].GetTypeInfo()))
                        {
                            matches = false;
                            break;
                        }
                    }
                    if (matches)
                    {
                        return constructorInfo;
                    }
                }
            }
            return null;
        }

        private static PropertyInfo GetPublicInstancePropertyInfo(Type type, string name)
        {
            Type typeToCheck = type;
            PropertyInfo propertyInfo = null;
            while (propertyInfo == null && typeToCheck != null)
            {
                TypeInfo typeInfo = typeToCheck.GetTypeInfo();
                propertyInfo = typeInfo.DeclaredProperties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p.GetMethod.Attributes.HasFlag(MethodAttributes.Public) && !p.GetMethod.Attributes.HasFlag(MethodAttributes.Static));
                typeToCheck = typeInfo.BaseType;
            }
            return propertyInfo;
        }

        private static FieldInfo GetPublicInstanceFieldInfo(Type type, string name)
        {
            Type typeToCheck = type;
            FieldInfo fieldInfo = null;
            while (fieldInfo == null && typeToCheck != null)
            {
                TypeInfo typeInfo = typeToCheck.GetTypeInfo();
                fieldInfo = typeInfo.DeclaredFields.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.IsPublic && !f.IsStatic);
                typeToCheck = typeInfo.BaseType;
            }
            return fieldInfo;
        }
    }
}
