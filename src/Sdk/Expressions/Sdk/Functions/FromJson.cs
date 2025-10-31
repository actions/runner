#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.IO;
using GitHub.Actions.Expressions.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.Expressions.Sdk.Functions
{
    internal sealed class FromJson : Function
    {
        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var json = Parameters[0].Evaluate(context).ConvertToString();

            if (context.Options.StrictJsonParsing)
            {
                try
                {
                    return JsonParser.Parse(json);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    throw new System.Text.Json.JsonException($"Error parsing fromJson: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new System.Text.Json.JsonException($"Unexpected error parsing fromJson: {ex.Message}", ex);
                }
            }

            try
            {
                using var stringReader = new StringReader(json);
                using var jsonReader = new JsonTextReader(stringReader) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double };
                var token = JToken.ReadFrom(jsonReader);
                return token.ToExpressionData();
            }
            catch (JsonReaderException ex)
            {
                throw new JsonReaderException("Error parsing fromJson", ex);
            }
        }
    }
}
