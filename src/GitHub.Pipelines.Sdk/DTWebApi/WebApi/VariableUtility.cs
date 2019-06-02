using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public static class VariableUtility
    {
        public static EnableAccessTokenType GetEnableAccessTokenType(IDictionary<String, VariableValue> variables)
        {
            EnableAccessTokenType type;
            if (variables != null &&
                variables.TryGetValue(WellKnownDistributedTaskVariables.EnableAccessToken, out VariableValue enableVariable) &&
                enableVariable != null)
            {
                Enum.TryParse(enableVariable.Value, true, out type);
            }
            else
            {
                type = EnableAccessTokenType.None;
            }

            return type;
        }

        public static Boolean IsVariable(String value)
        {
            return s_variableReferenceRegex.Value.IsMatch(value);
        }

        /// <summary>
        /// Replaces variables by recursively cloning tokens in a JObject or JArray by 
        /// Walks tokens and uses ExpandVariables(string, vars) to resolve all string tokens
        /// </summary>
        /// <param name="token">root token must be a JObject or JArray</param>
        /// <param name="replacementDictionary">key value variables to replace in the $(xxx) format</param>
        /// <returns>root token of cloned tree</returns>
        public static JToken ExpandVariables(JToken token, IDictionary<string, string> replacementDictionary, bool useMachineVariables = true)
        {
            var mapFuncs = new Dictionary<JTokenType, Func<JToken, JToken>>
            {
                {
                    JTokenType.String,
                    (t) => VariableUtility.ExpandVariables(t.ToString(), replacementDictionary, useMachineVariables)
                }
            };

            return token.Map(mapFuncs);
        }

        public static JToken ExpandVariables(
           JToken token,
           VariablesDictionary additionalVariableReplacements,
           Boolean useMachineVariables)
        {
            return ExpandVariables(token, (IDictionary<String, String>)additionalVariableReplacements, useMachineVariables);
        }

        /// <summary>
        /// Replaces multiple variable sets by recursively cloning tokens in a JObject or JArray.
        /// Walks tokens and uses ExpandVariables(string, vars) for each set of variables on all string tokens
        /// </summary>
        /// <param name="token">root token must be a JObject or JArray</param>
        /// <param name="replacementsList">list of replacement key value pairs in the $(xxx) format</param>
        /// <returns>root token of cloned tree</returns>
        public static JToken ExpandVariables(JToken token, IList<IDictionary<string, string>> replacementsList)
        {
            var mapFuncs = new Dictionary<JTokenType, Func<JToken, JToken>>
            {
                {
                    JTokenType.String,
                    (t) => replacementsList.Aggregate(t, (current, replacementVariables) => ExpandVariables(current.ToString(), replacementVariables))
                }
            };

            return token.Map(mapFuncs);
        }

        /// <summary>
        /// An overload method for ExpandVariables
        /// Expand variables in the input provided using the dictionary and the machine's environment variables
        /// </summary>
        public static String ExpandVariables(String input, IDictionary<String, String> additionalVariableReplacements)
        {
            return ExpandVariables(input, additionalVariableReplacements, true);
        }

        /// <summary>
        /// Replaces variable references of the form $(variable) with the corresponding replacement value. Values 
        /// populated into the environment directly are used first. If no value is found in the automation environment
        /// then the machine environment variables will be used as a fall back.
        /// </summary>
        /// <param name="input">The value which should be analyzed for environment variables and updated accordingly</param>
        /// <param name="useMachineVariables">Use the machine's environment variables when it is true</param>
        /// <returns>A new value with all variables expanded to their current value based on the environment</returns>
        public static String ExpandVariables(String input, IDictionary<String, String> additionalVariableReplacements, bool useMachineVariables)
        {
            // Do a quick up-front check against a regular expression to determine whether or not there is any
            // reason to allocate memory to replace values in the input
            if (!s_variableReferenceRegex.Value.IsMatch(input))
            {
                return input;
            }

            StringBuilder sb = new StringBuilder(input);
            List<String> referencedVariables = GetReferencedVariables(input);
            for (Int32 i = 0; i < referencedVariables.Count; i++)
            {
                // The variable reference is of the format $(variable), so we start at index 2 and cut off the last ')'
                // character by extracting a length of 3 less than the original length.
                String variableName = referencedVariables[i].Substring(2, referencedVariables[i].Length - 3);

                String replacementValue;
                if (!additionalVariableReplacements.TryGetValue(variableName, out replacementValue) && useMachineVariables)
                {
                    replacementValue = System.Environment.GetEnvironmentVariable(variableName);
                }

                if (replacementValue != null)
                {
                    sb.Replace(referencedVariables[i], replacementValue);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Replaces variable references of the form $(variable) with the corresponding replacement value. Values 
        /// populated into the environment directly are used first. If no value is found in the automation environment
        /// then the machine environment variables will be used as a fall back.
        /// </summary>
        /// <param name="input">The value which should be analyzed for environment variables and updated accordingly</param>
        /// <param name="useMachineVariables">Use the machine's environment variables when it is true</param>
        /// <returns>A new value with all variables expanded to their current value based on the environment</returns>
        public static String ExpandVariables(
            String input,
            VariablesDictionary additionalVariableReplacements,
            Boolean useMachineVariables,
            Boolean maskSecrets = false)
        {
            return ExpandVariables(input, (IDictionary<String, VariableValue>)additionalVariableReplacements, useMachineVariables, maskSecrets);
        }

        /// <summary>
        /// Replaces variable references of the form $(variable) with the corresponding replacement value. Values 
        /// populated into the environment directly are used first. If no value is found in the automation environment
        /// then the machine environment variables will be used as a fall back.
        /// </summary>
        /// <param name="input">The value which should be analyzed for environment variables and updated accordingly</param>
        /// <param name="useMachineVariables">Use the machine's environment variables when it is true</param>
        /// <returns>A new value with all variables expanded to their current value based on the environment</returns>
        public static String ExpandVariables(
            String input,
            IDictionary<String, VariableValue> additionalVariableReplacements,
            Boolean useMachineVariables,
            Boolean maskSecrets = false)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            StringBuilder sb = new StringBuilder(input);
            List<String> referencedVariables = GetReferencedVariables(input);
            for (Int32 i = 0; i < referencedVariables.Count; i++)
            {
                // The variable reference is of the format $(variable), so we start at index 2 and cut off the last ')'
                // character by extracting a length of 3 less than the original length.
                String variableName = referencedVariables[i].Substring(2, referencedVariables[i].Length - 3);

                VariableValue replacementValue;
                if (!additionalVariableReplacements.TryGetValue(variableName, out replacementValue) && useMachineVariables)
                {
                    replacementValue = new VariableValue { Value = System.Environment.GetEnvironmentVariable(variableName) };
                }

                if (replacementValue != null)
                {
                    var value = replacementValue.Value;
                    if (replacementValue.IsSecret && maskSecrets)
                    {
                        value = "***";
                    }

                    sb.Replace(referencedVariables[i], value);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Replaces variable references of the form variables['variable_name'] with corresponding replacement values
        /// </summary>
        /// <param name="condition">Task condition</param>
        /// <param name="additionalVariableReplacements">List of variables and their replacement values</param>
        /// <returns></returns>
        public static String ExpandConditionVariables(String condition, IDictionary<String, String> additionalVariableReplacements, bool useMachineVariables)
        {
            // Do a quick up-front check against a regular expression to determine whether or not there is any
            // reason to allocate memory to replace values in the input
            if (!s_conditionVariableReferenceRegex.Value.IsMatch(condition))
            {
                return condition;
            }

            StringBuilder sb = new StringBuilder(condition);
            MatchCollection matches = s_conditionVariableReferenceRegex.Value.Matches(condition);

            for (Int32 i = 0; i < matches.Count; i++)
            {
                if (matches[i].Length != 0 && matches[i].Groups.Count >= 2)
                {
                    String referencedVariable = matches[i].Groups[0].Value;
                    String variableName = matches[i].Groups[1].Value;

                    String replacementValue;
                    if (!additionalVariableReplacements.TryGetValue(variableName, out replacementValue) && useMachineVariables)
                    {
                        replacementValue = System.Environment.GetEnvironmentVariable(variableName);
                    }

                    if (replacementValue != null)
                    {
                        string convertedValue = PrepareReplacementStringForConditions(replacementValue);
                        sb.Replace(referencedVariable, convertedValue);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prepare replacement string from the given input. For a normal input, add ' around it.
        /// Convert a variable of format ${var} to variables['var'] to suit custom conditions
        /// </summary>
        /// <param name="replacementValue">input replacement value</param>
        /// <returns></returns>
        public static String PrepareReplacementStringForConditions(String replacementValue)
        {
            if (replacementValue == null || !IsVariable(replacementValue))
            {
                return String.Format(CultureInfo.InvariantCulture, c_conditionReplacementFormat, replacementValue);
            }

            List<String> variables = GetReferencedVariables(replacementValue);

            if (variables.Count != 1 || replacementValue.Trim() != variables[0])
            {
                return String.Format(CultureInfo.InvariantCulture, c_conditionReplacementFormat, replacementValue);
            }

            // Start from index 2 [after $( ] and continue till last but one
            string variableName = variables[0].Substring(2, variables[0].Length - 3);
            return string.Format(CultureInfo.InvariantCulture, c_conditionVariableFormat, variableName);
        }

        private static List<String> GetReferencedVariables(String input)
        {
            Int32 nestedCount = -1;
            Boolean insideMatch = false;
            StringBuilder currentMatch = new StringBuilder();
            HashSet<String> result = new HashSet<String>();
            for (int i = 0; i < input.Length; i++)
            {
                if (!insideMatch && input[i] == '$' && i + 1 < input.Length && input[i + 1] == '(')
                {
                    insideMatch = true;
                }

                if (insideMatch)
                {
                    currentMatch.Append(input[i]);
                }

                if (insideMatch && input[i] == '(')
                {
                    nestedCount++;
                }

                if (insideMatch && input[i] == ')')
                {
                    if (nestedCount == 0)
                    {
                        result.Add(currentMatch.ToString());
                        currentMatch.Clear();
                        insideMatch = false;
                        nestedCount = -1;
                    }
                    else
                    {
                        nestedCount--;
                    }
                }
            }

            if (insideMatch || nestedCount >= 0)
            {
                // We didn't finish the last match, that means it isn't correct so we will ignore it
                Debug.Fail("We didn't finish the last match!!!!!");
            }

            return result.ToList();
        }

        private static readonly Lazy<Regex> s_variableReferenceRegex = new Lazy<Regex>(() => new Regex(@"\$\(([^)]+)\)", RegexOptions.Singleline | RegexOptions.Compiled), true);
        private static readonly Lazy<Regex> s_conditionVariableReferenceRegex = new Lazy<Regex>(() => new Regex(@"variables\['([^']+)\']", RegexOptions.Singleline | RegexOptions.Compiled), true);
        private const String c_conditionReplacementFormat = "'{0}'";
        private const String c_variableFormat = "$({0})";
        private const String c_conditionVariableFormat = "variables['{0}']";
    }
}