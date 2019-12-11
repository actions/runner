using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism for performing delayed evaluation of a value based on the environment context as runtime.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ExpressionValue
    {
        public static Boolean IsExpression(String value)
        {
            return !String.IsNullOrEmpty(value) &&
                   value.Length > 3 &&
                   value.StartsWith("$[", StringComparison.Ordinal) &&
                   value.EndsWith("]", StringComparison.Ordinal);
        }

        /// <summary>
        /// Attempts to parse the specified string as an expression value.
        /// </summary>
        /// <typeparam name="T">The expected type of the expression result</typeparam>
        /// <param name="expression">The expression string</param>
        /// <param name="value">The value which was parsed, if any</param>
        /// <returns>True if the value was successfully parsed; otherwise, false</returns>
        public static Boolean TryParse<T>(
            String expression,
            out ExpressionValue<T> value)
        {
            if (IsExpression(expression))
            {
                value = new ExpressionValue<T>(expression, isExpression: true);
            }
            else
            {
                value = null;
            }
            return value != null;
        }

        /// <summary>
        /// Creates an ExpressionValue from expression string. 
        /// Returns null if argument is not an expression
        /// </summary>
        public static ExpressionValue<T> FromExpression<T>(String expression)
        {
            return new ExpressionValue<T>(expression, isExpression: true);
        }

        /// <summary>
        /// Creates an ExpressionValue from literal. 
        /// </summary>
        public static ExpressionValue<T> FromLiteral<T>(T literal)
        {
            return new ExpressionValue<T>(literal);
        }

        /// <summary>
        /// When T is String, we cannot distiguish between literals and expressions solely by type. 
        /// Use this function when parsing and you want to err on the side of expressions.
        /// </summary>
        public static ExpressionValue<String> FromToken(String token)
        {
            if (ExpressionValue.IsExpression(token))
            {
                return ExpressionValue.FromExpression<String>(token);
            }

            return ExpressionValue.FromLiteral(token);
        }

        internal static String TrimExpression(String value)
        {
            var expression = value.Substring(2, value.Length - 3).Trim();
            if (String.IsNullOrEmpty(expression))
            {
                throw new ArgumentException(PipelineStrings.ExpressionInvalid(value));
            }
            return expression;
        }
    }

    /// <summary>
    /// Provides a mechanism for performing delayed evaluation of a value based on the environment context at runtime.
    /// </summary>
    /// <typeparam name="T">The type of value</typeparam>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ExpressionValue<T> : ExpressionValue, IEquatable<ExpressionValue<T>>
    {
        /// <summary>
        /// Initializes a new <c>ExpressionValue</c> instance with the specified literal value.
        /// </summary>
        /// <param name="literalValue">The literal value which should be used</param>
        public ExpressionValue(T literalValue)
        {
            m_literalValue = literalValue;
        }

        /// <summary>
        /// Initializes a new <c>ExpressionValue</c> with the given expression. 
        /// Throws if expression is invalid. 
        /// </summary>
        /// <param name="expression">The expression to be used</param>
        /// <param name="isExpression">This parameter is unused other than to discriminate this constructor from the literal constructor</param>
        internal ExpressionValue(
            String expression,
            Boolean isExpression)
        {
            if (!IsExpression(expression))
            {
                throw new ArgumentException(PipelineStrings.ExpressionInvalid(expression));
            }
            m_expression = ExpressionValue.TrimExpression(expression);
        }

        [JsonConstructor]
        private ExpressionValue()
        {
        }

        internal T Literal
        {
            get
            {
                return m_literalValue;
            }
        }

        internal String Expression
        {
            get
            {
                return m_expression;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the expression is backed by a literal value.
        /// </summary>
        internal Boolean IsLiteral => String.IsNullOrEmpty(m_expression);

        /// <summary>
        /// Converts the value to a string representation.
        /// </summary>
        /// <returns>A string representation of the current value</returns>
        public override String ToString()
        {
            if (!String.IsNullOrEmpty(m_expression))
            {
                return String.Concat("$[ ", m_expression, " ]");
            }
            else
            {
                return m_literalValue?.ToString();
            }
        }

        /// <summary>
        /// Provides automatic conversion of a literal value into a pipeline value for convenience.
        /// </summary>
        /// <param name="value">The value which the pipeline value represents</param>
        public static implicit operator ExpressionValue<T>(T value)
        {
            return new ExpressionValue<T>(value);
        }

        public Boolean Equals(ExpressionValue<T> rhs)
        {
            if (rhs is null)
            {
                return false;
            }

            if (ReferenceEquals(this, rhs))
            {
                return true;
            }

            if (IsLiteral)
            {
                return EqualityComparer<T>.Default.Equals(this.Literal, rhs.Literal);
            }
            else
            {
                return this.Expression == rhs.Expression;
            }
        }

        public override Boolean Equals(object obj)
        {
            return Equals(obj as ExpressionValue<T>);
        }

        public static Boolean operator ==(ExpressionValue<T> lhs, ExpressionValue<T> rhs)
        {
            if (lhs is null)
            {
                return rhs is null;
            }

            return lhs.Equals(rhs);
        }
        public static Boolean operator !=(ExpressionValue<T> lhs, ExpressionValue<T> rhs)
        {
            return !(lhs == rhs);
        }

        public override Int32 GetHashCode()
        {
            if (IsLiteral)
            {
                if (Literal != null)
                {
                    return Literal.GetHashCode();
                }
            }
            else if (Expression != null)
            {
                return Expression.GetHashCode();
            }

            return 0; // unspecified expression values are all the same.
        }

        [DataMember(Name = "LiteralValue", EmitDefaultValue = false)]
        private readonly T m_literalValue;

        [DataMember(Name = "VariableValue", EmitDefaultValue = false)]
        private readonly String m_expression;
    }

    internal class ExpressionValueJsonConverter<T> : VssSecureJsonConverter
    {
        public override Boolean CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().Equals(typeof(String).GetTypeInfo()) || typeof(T).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                // string types are either expressions of any type T, or literals of type String
                var s = (String)(Object)reader.Value;
                if (ExpressionValue.IsExpression(s))
                {
                    return ExpressionValue.FromExpression<T>(s);
                }
                else
                {
                    return new ExpressionValue<String>(s);
                }
            }
            else
            {
                var parsedValue = serializer.Deserialize<T>(reader);
                return new ExpressionValue<T>(parsedValue);
            }
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);
            if (value is ExpressionValue<T> expressionValue)
            {
                if (!String.IsNullOrEmpty(expressionValue.Expression))
                {
                    serializer.Serialize(writer, $"$[ {expressionValue.Expression} ]");
                }
                else
                {
                    serializer.Serialize(writer, expressionValue.Literal);
                }
            }
        }
    }
}
