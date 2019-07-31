using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Reflection;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using System.Globalization;
using System.Linq;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionManifestManager))]
    public interface IActionManifestManager : IRunnerService
    {
        ActionDefinitionData Load(IExecutionContext executionContext, string manifestFile);

        List<string> EvaluateContainerArguments(IExecutionContext executionContext, SequenceToken token, IDictionary<string, PipelineContextData> contextData);

        Dictionary<string, string> EvaluateContainerEnvironment(IExecutionContext executionContext, MappingToken token, IDictionary<string, PipelineContextData> contextData);
    }

    public sealed class ActionManifestManager : RunnerService, IActionManifestManager
    {
        private TemplateSchema _actionManifestSchema;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            var assembly = Assembly.GetExecutingAssembly();
            var json = default(string);
            using (var stream = assembly.GetManifestResourceStream("GitHub.Runner.Worker.action_yaml.json"))
            using (var streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            var objectReader = new JsonObjectReader(null, json);
            _actionManifestSchema = TemplateSchema.Load(objectReader);
            ArgUtil.NotNull(_actionManifestSchema, nameof(_actionManifestSchema));
            Trace.Info($"Load schema file with definitions: {StringUtil.ConvertToJson(_actionManifestSchema.Definitions.Keys)}");
        }

        public ActionDefinitionData Load(IExecutionContext executionContext, string manifestFile)
        {
            var context = CreateContext(executionContext, null);
            ActionDefinitionData actionDefinition = new ActionDefinitionData();
            try
            {
                var token = default(TemplateToken);

                // Get the file ID
                var fileId = context.GetFileId(manifestFile);
                var fileContent = File.ReadAllText(manifestFile);
                using (var stringReader = new StringReader(fileContent))
                {
                    var yamlObjectReader = new YamlObjectReader(null, stringReader);
                    token = TemplateReader.Read(context, "action-root", yamlObjectReader, fileId, out _);
                }

                var actionMapping = token.AssertMapping("action manifest root");
                foreach (var actionPair in actionMapping)
                {
                    var propertyName = actionPair.Key.AssertString($"action.yml property key");

                    switch (propertyName.Value)
                    {
                        case "name":
                            actionDefinition.Name = actionPair.Value.AssertString("name").Value;
                            break;

                        case "description":
                            actionDefinition.Description = actionPair.Value.AssertString("description").Value;
                            break;

                        case "inputs":
                            actionDefinition.Inputs = ConvertInputs(context, actionPair.Value);
                            break;

                        case "runs":
                            actionDefinition.Execution = ConvertRuns(context, actionPair.Value);
                            break;
                        default:
                            Trace.Info($"Ignore action property {propertyName}.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                context.Errors.Add(ex);
            }

            if (context.Errors.Count > 0)
            {
                foreach (var error in context.Errors)
                {
                    Trace.Error($"Action.yml load error: {error.Message}");
                    executionContext.Error(error.Message);
                }

                throw new ArgumentException($"Fail to load {manifestFile}");
            }

            if (actionDefinition.Execution == null)
            {
                executionContext.Debug($"Loaded action.yml file: {StringUtil.ConvertToJson(actionDefinition)}");
                throw new ArgumentException($"Top level 'run:' section is required for {manifestFile}");
            }
            else
            {
                Trace.Info($"Loaded action.yml file: {StringUtil.ConvertToJson(actionDefinition)}");
            }

            return actionDefinition;
        }

        public List<string> EvaluateContainerArguments(
            IExecutionContext executionContext,
            SequenceToken token,
            IDictionary<string, PipelineContextData> contextData)
        {
            var result = new List<string>();

            if (token != null)
            {
                var context = CreateContext(executionContext, contextData);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(context, "container-runs-args", token, 0, null, omitHeader: true);
                    context.Errors.Check();

                    Trace.Info($"Arguments evaluate result: {StringUtil.ConvertToJson(evaluateResult)}");

                    // Sequence
                    var args = evaluateResult.AssertSequence("container args");

                    foreach (var arg in args)
                    {
                        var str = arg.AssertString("container arg").Value;
                        result.Add(str);
                        Trace.Info($"Add argument {str}");
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    Trace.Error(ex);
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Dictionary<string, string> EvaluateContainerEnvironment(
            IExecutionContext executionContext,
            MappingToken token,
            IDictionary<string, PipelineContextData> contextData)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (token != null)
            {
                var context = CreateContext(executionContext, contextData);
                try
                {
                    var evaluateResult = TemplateEvaluator.Evaluate(context, "container-runs-env", token, 0, null, omitHeader: true);
                    context.Errors.Check();

                    Trace.Info($"Environments evaluate result: {StringUtil.ConvertToJson(evaluateResult)}");

                    // Mapping
                    var mapping = evaluateResult.AssertMapping("container env");

                    foreach (var pair in mapping)
                    {
                        // Literal key
                        var key = pair.Key.AssertString("container env key");

                        // Literal value
                        var value = pair.Value.AssertString("container env value");
                        result[key.Value] = value.Value;

                        Trace.Info($"Add env {key} = {value}");
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    Trace.Error(ex);
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        private TemplateContext CreateContext(
            IExecutionContext executionContext,
            IDictionary<string, PipelineContextData> contextData)
        {
            var result = new TemplateContext
            {
                CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(10, 500),
                Memory = new TemplateMemory(
                    maxDepth: 100,
                    maxEvents: 1000000,
                    maxBytes: 10 * 1024 * 1024),
                Schema = _actionManifestSchema,
                TraceWriter = executionContext.ToTemplateTraceWriter(),
            };

            if (contextData?.Count > 0)
            {
                foreach (var pair in contextData)
                {
                    result.ExpressionValues[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        private ActionExecutionData ConvertRuns(
            TemplateContext context,
            TemplateToken inputsToken)
        {
            var runsMapping = inputsToken.AssertMapping("runs");
            var usingToken = default(StringToken);
            var imageToken = default(StringToken);
            var argsToken = default(SequenceToken);
            var entrypointToken = default(StringToken);
            var envToken = default(MappingToken);
            var mainToken = default(StringToken);
            var pluginToken = default(StringToken);
            foreach (var run in runsMapping)
            {
                var runsKey = run.Key.AssertString("runs key").Value;
                switch (runsKey)
                {
                    case "using":
                        usingToken = run.Value.AssertString("using");
                        break;
                    case "image":
                        imageToken = run.Value.AssertString("image");
                        break;
                    case "args":
                        argsToken = run.Value.AssertSequence("args");
                        break;
                    case "entrypoint":
                        entrypointToken = run.Value.AssertString("entrypoint");
                        break;
                    case "env":
                        envToken = run.Value.AssertMapping("env");
                        break;
                    case "main":
                        mainToken = run.Value.AssertString("main");
                        break;
                    case "plugin":
                        pluginToken = run.Value.AssertString("plugin");
                        break;
                    default:
                        Trace.Info($"Ignore run property {runsKey}.");
                        break;
                }
            }

            if (usingToken != null)
            {
                if (string.Equals(usingToken.Value, "docker", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(imageToken?.Value))
                    {
                        throw new ArgumentNullException($"Image is not provided.");
                    }
                    else
                    {
                        return new ContainerActionExecutionData()
                        {
                            Image = imageToken.Value,
                            Arguments = argsToken,
                            EntryPoint = entrypointToken?.Value,
                            Environment = envToken
                        };
                    }
                }
                else if (string.Equals(usingToken.Value, "node12", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(mainToken?.Value))
                    {
                        throw new ArgumentNullException($"Entry javascript fils is not provided.");
                    }
                    else
                    {
                        return new NodeJSActionExecutionData()
                        {
                            Script = mainToken.Value
                        };
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"'using: {usingToken.Value}' is not supported, use 'docker' or 'node12' instead.");
                }
            }
            else if (pluginToken != null)
            {
                return new PluginActionExecutionData()
                {
                    Plugin = pluginToken.Value
                };
            }

            throw new NotSupportedException(nameof(ConvertRuns));
        }

        private MappingToken ConvertInputs(
            TemplateContext context,
            TemplateToken inputsToken)
        {
            var result = new MappingToken(null, null, null);
            var inputsMapping = inputsToken.AssertMapping("inputs");
            foreach (var input in inputsMapping)
            {
                bool hasDefault = false;
                var inputName = input.Key.AssertString("input name");
                var inputMetadata = input.Value.AssertMapping("input metadata");
                foreach (var metadata in inputMetadata)
                {
                    var metadataName = metadata.Key.AssertString("input metadata").Value;
                    if (string.Equals(metadataName, "default", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDefault = true;
                        var inputDefault = metadata.Value.AssertString("input default");
                        result.Add(inputName, inputDefault);
                        break;
                    }
                }

                if (!hasDefault)
                {
                    result.Add(inputName, new StringToken(null, null, null, string.Empty));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Converts a YAML file into a TemplateToken
    /// </summary>
    internal sealed class YamlObjectReader : IObjectReader
    {
        internal YamlObjectReader(
            Int32? fileId,
            TextReader input)
        {
            m_fileId = fileId;
            m_parser = new Parser(input);
        }

        public Boolean AllowLiteral(out LiteralToken value)
        {
            if (EvaluateCurrent() is Scalar scalar)
            {
                // Tag specified
                if (!string.IsNullOrEmpty(scalar.Tag))
                {
                    // String tag
                    if (string.Equals(scalar.Tag, c_stringTag, StringComparison.Ordinal))
                    {
                        value = new StringToken(m_fileId, scalar.Start.Line, scalar.Start.Column, scalar.Value);
                        MoveNext();
                        return true;
                    }

                    // Not plain style
                    if (scalar.Style != ScalarStyle.Plain)
                    {
                        throw new NotSupportedException($"The scalar style '{scalar.Style}' on line {scalar.Start.Line} and column {scalar.Start.Column} is not valid with the tag '{scalar.Tag}'");
                    }

                    // Boolean, Float, Integer, or Null
                    switch (scalar.Tag)
                    {
                        case c_booleanTag:
                            value = ParseBoolean(scalar);
                            break;
                        case c_floatTag:
                            value = ParseFloat(scalar);
                            break;
                        case c_integerTag:
                            value = ParseInteger(scalar);
                            break;
                        case c_nullTag:
                            value = ParseNull(scalar);
                            break;
                        default:
                            throw new NotSupportedException($"Unexpected tag '{scalar.Tag}'");
                    }

                    MoveNext();
                    return true;
                }

                // Plain style, determine type using YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
                if (scalar.Style == ScalarStyle.Plain)
                {
                    if (MatchNull(scalar, out var nullToken))
                    {
                        value = nullToken;
                    }
                    else if (MatchBoolean(scalar, out var booleanToken))
                    {
                        value = booleanToken;
                    }
                    else if (MatchInteger(scalar, out var numberToken) ||
                        MatchFloat(scalar, out numberToken))
                    {
                        value = numberToken;
                    }
                    else
                    {
                        value = new StringToken(m_fileId, scalar.Start.Line, scalar.Start.Column, scalar.Value);
                    }

                    MoveNext();
                    return true;
                }

                // Otherwise assume string
                value = new StringToken(m_fileId, scalar.Start.Line, scalar.Start.Column, scalar.Value);
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowSequenceStart(out SequenceToken value)
        {
            if (EvaluateCurrent() is SequenceStart sequenceStart)
            {
                value = new SequenceToken(m_fileId, sequenceStart.Start.Line, sequenceStart.Start.Column);
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowSequenceEnd()
        {
            if (EvaluateCurrent() is SequenceEnd)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        public Boolean AllowMappingStart(out MappingToken value)
        {
            if (EvaluateCurrent() is MappingStart mappingStart)
            {
                value = new MappingToken(m_fileId, mappingStart.Start.Line, mappingStart.Start.Column);
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowMappingEnd()
        {
            if (EvaluateCurrent() is MappingEnd)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumes the last parsing events, which are expected to be DocumentEnd and StreamEnd.
        /// </summary>
        public void ValidateEnd()
        {
            if (EvaluateCurrent() is DocumentEnd)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected document end parse event");
            }

            if (EvaluateCurrent() is StreamEnd)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected stream end parse event");
            }

            if (MoveNext())
            {
                throw new InvalidOperationException("Expected end of parse events");
            }
        }

        /// <summary>
        /// Consumes the first parsing events, which are expected to be StreamStart and DocumentStart.
        /// </summary>
        public void ValidateStart()
        {
            if (EvaluateCurrent() != null)
            {
                throw new InvalidOperationException("Unexpected parser state");
            }

            if (!MoveNext())
            {
                throw new InvalidOperationException("Expected a parse event");
            }

            if (EvaluateCurrent() is StreamStart)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected stream start parse event");
            }

            if (EvaluateCurrent() is DocumentStart)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected document start parse event");
            }
        }

        private ParsingEvent EvaluateCurrent()
        {
            if (m_current == null)
            {
                m_current = m_parser.Current;
                if (m_current != null)
                {
                    if (m_current is Scalar scalar)
                    {
                        // Verify not using achors
                        if (scalar.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{scalar.Anchor}'");
                        }
                    }
                    else if (m_current is MappingStart mappingStart)
                    {
                        // Verify not using achors
                        if (mappingStart.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{mappingStart.Anchor}'");
                        }
                    }
                    else if (m_current is SequenceStart sequenceStart)
                    {
                        // Verify not using achors
                        if (sequenceStart.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{sequenceStart.Anchor}'");
                        }
                    }
                    else if (!(m_current is MappingEnd) &&
                        !(m_current is SequenceEnd) &&
                        !(m_current is DocumentStart) &&
                        !(m_current is DocumentEnd) &&
                        !(m_current is StreamStart) &&
                        !(m_current is StreamEnd))
                    {
                        throw new InvalidOperationException($"Unexpected parsing event type: {m_current.GetType().Name}");
                    }
                }
            }

            return m_current;
        }

        private Boolean MoveNext()
        {
            m_current = null;
            return m_parser.MoveNext();
        }

        private BooleanToken ParseBoolean(Scalar scalar)
        {
            if (MatchBoolean(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_booleanTag); // throws
            return default;
        }

        private NumberToken ParseFloat(Scalar scalar)
        {
            if (MatchFloat(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_floatTag); // throws
            return default;
        }

        private NumberToken ParseInteger(Scalar scalar)
        {
            if (MatchInteger(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_integerTag); // throws
            return default;
        }

        private NullToken ParseNull(Scalar scalar)
        {
            if (MatchNull(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_nullTag); // throws
            return default;
        }

        private Boolean MatchBoolean(
            Scalar scalar,
            out BooleanToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            switch (scalar.Value ?? string.Empty)
            {
                case "true":
                case "True":
                case "TRUE":
                    value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, true);
                    return true;
                case "false":
                case "False":
                case "FALSE":
                    value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, false);
                    return true;
            }

            value = default;
            return false;
        }

        private Boolean MatchFloat(
            Scalar scalar,
            out NumberToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            var str = scalar.Value;
            if (!string.IsNullOrEmpty(str))
            {
                // Check for [-+]?(\.inf|\.Inf|\.INF)|\.nan|\.NaN|\.NAN
                switch (str)
                {
                    case ".inf":
                    case ".Inf":
                    case ".INF":
                    case "+.inf":
                    case "+.Inf":
                    case "+.INF":
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.PositiveInfinity);
                        return true;
                    case "-.inf":
                    case "-.Inf":
                    case "-.INF":
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.NegativeInfinity);
                        return true;
                    case ".nan":
                    case ".NaN":
                    case ".NAN":
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.NaN);
                        return true;
                }

                // Otherwise check [-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?

                // Skip leading sign
                var index = str[0] == '-' || str[0] == '+' ? 1 : 0;

                // Check for integer portion
                var length = str.Length;
                var hasInteger = false;
                while (index < length && str[index] >= '0' && str[index] <= '9')
                {
                    hasInteger = true;
                    index++;
                }

                // Check for decimal point
                var hasDot = false;
                if (index < length && str[index] == '.')
                {
                    hasDot = true;
                    index++;
                }

                // Check for decimal portion
                var hasDecimal = false;
                while (index < length && str[index] >= '0' && str[index] <= '9')
                {
                    hasDecimal = true;
                    index++;
                }

                // Check [-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)
                if ((hasDot && hasDecimal) || hasInteger)
                {
                    // Check for end
                    if (index == length)
                    {
                        // Try parse
                        if (Double.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue))
                        {
                            value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue);
                            return true;
                        }
                        // Otherwise exceeds range
                        else
                        {
                            ThrowInvalidValue(scalar, c_floatTag); // throws
                        }
                    }
                    // Check [eE][-+]?[0-9]
                    else if (index < length && (str[index] == 'e' || str[index] == 'E'))
                    {
                        index++;

                        // Skip sign
                        if (index < length && (str[index] == '-' || str[index] == '+'))
                        {
                            index++;
                        }

                        // Check for exponent
                        var hasExponent = false;
                        while (index < length && str[index] >= '0' && str[index] <= '9')
                        {
                            hasExponent = true;
                            index++;
                        }

                        // Check for end
                        if (hasExponent && index == length)
                        {
                            // Try parse
                            if (Double.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var doubleValue))
                            {
                                value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, (Double)doubleValue);
                                return true;
                            }
                            // Otherwise exceeds range
                            else
                            {
                                ThrowInvalidValue(scalar, c_floatTag); // throws
                            }
                        }
                    }
                }
            }

            value = default;
            return false;
        }

        private Boolean MatchInteger(
            Scalar scalar,
            out NumberToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            var str = scalar.Value;
            if (!string.IsNullOrEmpty(str))
            {
                // Check for [0-9]+
                var firstChar = str[0];
                if (firstChar >= '0' && firstChar <= '9' &&
                    str.Skip(1).All(x => x >= '0' && x <= '9'))
                {
                    // Try parse
                    if (Double.TryParse(str, NumberStyles.None, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue);
                        return true;
                    }

                    // Otherwise exceeds range
                    ThrowInvalidValue(scalar, c_integerTag); // throws
                }
                // Check for (-|+)[0-9]+
                else if ((firstChar == '-' || firstChar == '+') &&
                    str.Length > 1 &&
                    str.Skip(1).All(x => x >= '0' && x <= '9'))
                {
                    // Try parse
                    if (Double.TryParse(str, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue);
                        return true;
                    }

                    // Otherwise exceeds range
                    ThrowInvalidValue(scalar, c_integerTag); // throws
                }
                // Check for 0x[0-9a-fA-F]+
                else if (firstChar == '0' &&
                    str.Length > 2 &&
                    str[1] == 'x' &&
                    str.Skip(2).All(x => (x >= '0' && x <= '9') || (x >= 'a' && x <= 'f') || (x >= 'A' && x <= 'F')))
                {
                    // Try parse
                    if (Int32.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var integerValue))
                    {
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, integerValue);
                        return true;
                    }

                    // Otherwise exceeds range
                    ThrowInvalidValue(scalar, c_integerTag); // throws
                }
                // Check for 0o[0-9]+
                else if (firstChar == '0' &&
                    str.Length > 2 &&
                    str[1] == 'o' &&
                    str.Skip(2).All(x => x >= '0' && x <= '7'))
                {
                    // Try parse
                    var integerValue = default(Int32);
                    try
                    {
                        integerValue = Convert.ToInt32(str.Substring(2), 8);
                    }
                    // Otherwise exceeds range
                    catch (Exception)
                    {
                        ThrowInvalidValue(scalar, c_integerTag); // throws
                    }

                    value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, integerValue);
                    return true;
                }
            }

            value = default;
            return false;
        }

        private Boolean MatchNull(
            Scalar scalar,
            out NullToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            switch (scalar.Value ?? string.Empty)
            {
                case "":
                case "null":
                case "Null":
                case "NULL":
                case "~":
                    value = new NullToken(m_fileId, scalar.Start.Line, scalar.Start.Column);
                    return true;
            }

            value = default;
            return false;
        }

        private void ThrowInvalidValue(
            Scalar scalar,
            String tag)
        {
            throw new NotSupportedException($"The value '{scalar.Value}' on line {scalar.Start.Line} and column {scalar.Start.Column} is invalid for the type '{scalar.Tag}'");
        }

        private const String c_booleanTag = "tag:yaml.org,2002:bool";
        private const String c_floatTag = "tag:yaml.org,2002:float";
        private const String c_integerTag = "tag:yaml.org,2002:int";
        private const String c_nullTag = "tag:yaml.org,2002:null";
        private const String c_stringTag = "tag:yaml.org,2002:string";
        private readonly Int32? m_fileId;
        private readonly Parser m_parser;
        private ParsingEvent m_current;
    }
}

