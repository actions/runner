using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.Expressions2.Tokens;
using System.IO;
using Runner.Server.Azure.Devops;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Converts a source object format into a TemplateToken
    /// </summary>
    internal sealed class TemplateReader
    {
        private TemplateReader(
            TemplateContext context,
            TemplateSchema schema,
            IObjectReader objectReader,
            Int32? fileId)
        {
            m_context = context;
            m_schema = schema;
            m_memory = context.Memory;
            m_objectReader = objectReader;
            m_fileId = fileId;
        }

        internal static TemplateToken Read(
            TemplateContext context,
            String type,
            IObjectReader objectReader,
            Int32? fileId,
            out Int32 bytes)
        {
            return Read(context, type, objectReader, fileId, context.Schema, out bytes);
        }

        internal static TemplateToken Read(
            TemplateContext context,
            String type,
            IObjectReader objectReader,
            Int32? fileId,
            TemplateSchema schema,
            out Int32 bytes)
        {
            TemplateToken result = null;

            var reader = new TemplateReader(context, schema, objectReader, fileId);
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                objectReader.ValidateStart();
                var definition = new DefinitionInfo(schema, type);
                result = reader.ReadValue(definition);
                objectReader.ValidateEnd();
            }
            catch (Exception ex)
            {
                context.Error(fileId, null, null, ex);
            }
            finally
            {
                bytes = context.Memory.CurrentBytes - originalBytes;
            }

            return result;
        }

        private bool Match(TemplateToken token) {
            if(token.PreWhiteSpace != null) {
                return m_context.Row > token.PreWhiteSpace.Line || m_context.Row == token.PreWhiteSpace.Line && m_context.Column >= token.PreWhiteSpace.Character;
            }
            return m_context.Row > token.Line || m_context.Row == token.Line && m_context.Column >= token.Column;
        }

        private bool MatchPost(TemplateToken token) {
            return m_context.Row < token.PostWhiteSpace.Line || m_context.Row == token.PostWhiteSpace.Line && m_context.Column <= token.PostWhiteSpace.Character;
        }

        private TemplateToken ReadValue(DefinitionInfo definition)
        {
            m_memory.IncrementEvents();

            // Scalar
            if (m_objectReader.AllowLiteral(out LiteralToken literal))
            {
                var scalar = ParseScalar(literal, definition);
                Validate(ref scalar, definition);
                m_memory.AddBytes(scalar);
                return scalar;
            }

            // Sequence
            if (m_objectReader.AllowSequenceStart(out SequenceToken sequence))
            {
                if(m_context.AutoCompleteMatches != null && Match(sequence)) {
                    m_context.AutoCompleteMatches.RemoveAll(m => m.Depth >= m_memory.Depth);
                    m_context.AutoCompleteMatches.Add(new AutoCompleteEntry {
                        Depth = m_memory.Depth,
                        Token = sequence,
                        AllowedContext = definition.AllowedContext,
                        Definitions = new [] { definition.Definition }
                    });
                }
                m_memory.IncrementDepth();
                m_memory.AddBytes(sequence);

                var sequenceDefinition = definition.Get<SequenceDefinition>().FirstOrDefault();

                // Legal
                if (sequenceDefinition != null)
                {
                    var itemDefinition = new DefinitionInfo(definition, sequenceDefinition.ItemType);

                    // Add each item
                    while (!m_objectReader.AllowSequenceEnd(sequence))
                    {
                        var item = ReadValue(itemDefinition);
                        sequence.Add(item);
                    }
                }
                // Illegal
                else
                {
                    // Error
                    m_context.Error(sequence, TemplateStrings.UnexpectedSequenceStart());

                    // Skip each item
                    while (!m_objectReader.AllowSequenceEnd(sequence))
                    {
                        SkipValue();
                    }
                }

                if(m_context.AutoCompleteMatches != null && sequence.PostWhiteSpace != null && !MatchPost(sequence)) {
                    var completion = m_context.AutoCompleteMatches.FirstOrDefault(m => m.Token == sequence);
                    if(completion != null) {
                        m_context.AutoCompleteMatches.RemoveAll(m => m.Depth >= completion.Depth);
                    }
                }

                m_memory.DecrementDepth();
                return sequence;
            }

            // Mapping
            if (m_objectReader.AllowMappingStart(out MappingToken mapping))
            {
                if(m_context.AutoCompleteMatches != null && Match(mapping)) {
                    m_context.AutoCompleteMatches.RemoveAll(m => m.Depth >= m_memory.Depth);
                    m_context.AutoCompleteMatches.Add(new AutoCompleteEntry {
                        Depth = m_memory.Depth,
                        Token = mapping,
                        AllowedContext = definition.AllowedContext,
                        Definitions = new [] { definition.Definition }
                    });
                }
                m_memory.IncrementDepth();
                m_memory.AddBytes(mapping);

                var mappingDefinitions = definition.Get<MappingDefinition>().ToList();

                // Legal
                if (mappingDefinitions.Count > 0)
                {
                    if (mappingDefinitions.Count > 1 ||
                        m_schema.HasProperties(mappingDefinitions[0]) ||
                        String.IsNullOrEmpty(mappingDefinitions[0].LooseKeyType))
                    {
                        HandleMappingWithWellKnownProperties(definition, mappingDefinitions, mapping);
                    }
                    else
                    {
                        var keyDefinition = new DefinitionInfo(definition, mappingDefinitions[0].LooseKeyType);
                        var valueDefinition = new DefinitionInfo(definition, mappingDefinitions[0].LooseValueType);
                        HandleMappingWithAllLooseProperties(definition, keyDefinition, valueDefinition, mapping);
                    }
                }
                // Illegal
                else
                {
                    m_context.Error(mapping, TemplateStrings.UnexpectedMappingStart());

                    while (!m_objectReader.AllowMappingEnd(mapping))
                    {
                        SkipValue();
                        SkipValue();
                    }
                }

                if(m_context.AutoCompleteMatches != null && mapping.PostWhiteSpace != null && !MatchPost(mapping)) {
                    var completion = m_context.AutoCompleteMatches.FirstOrDefault(m => m.Token == mapping);
                    if(completion != null) {
                        m_context.AutoCompleteMatches.RemoveAll(m => m.Depth >= completion.Depth);
                    }
                }

                m_memory.DecrementDepth();
                return mapping;
            }

            throw new InvalidOperationException(TemplateStrings.ExpectedScalarSequenceOrMapping());
        }

        private void HandleMappingWithWellKnownProperties(
            DefinitionInfo definition,
            List<MappingDefinition> mappingDefinitions,
            MappingToken mapping)
        {
            // Check if loose properties are allowed
            String looseKeyType = null;
            String looseValueType = null;
            DefinitionInfo? looseKeyDefinition = null;
            DefinitionInfo? looseValueDefinition = null;
            if (!String.IsNullOrEmpty(mappingDefinitions[0].LooseKeyType))
            {
                looseKeyType = mappingDefinitions[0].LooseKeyType;
                looseValueType = mappingDefinitions[0].LooseValueType;
            }

            var keys = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            var hasExpressionKey = false;
            int i = 0;
            while (m_objectReader.AllowLiteral(out LiteralToken rawLiteral))
            {
                var firstKey = i++ == 0;
                var nextKeyScalar = ParseScalar(rawLiteral, definition);
                // Expression
                if (nextKeyScalar is ExpressionToken)
                {
                    hasExpressionKey = true;
                    // Legal
                    if (definition.AllowedContext.Length > 0)
                    {
                        m_memory.AddBytes(nextKeyScalar);
                        TemplateToken nextValue;
                        var anyDefinition = new DefinitionInfo(definition, TemplateConstants.Any);
                        if(nextKeyScalar is EachExpressionToken eachexp) {
                            var def = m_context.AutoCompleteMatches != null ? new DefinitionInfo(definition) : new DefinitionInfo(definition, "any");
                            def.AllowedContext = definition.AllowedContext.Append(eachexp.Variable).ToArray();
                            if(m_context.AutoCompleteMatches != null && m_schema.Get<SequenceDefinition>(definition.Parent).Any()) {
                                var oneOf = new OneOfDefinition();
                                oneOf.OneOf.Add(definition.ParentName);
                                oneOf.OneOf.Add(definition.Name);
                                def.Definition = oneOf;
                            }
                            nextValue = ReadValue(def);
                        } else if(nextKeyScalar is ConditionalExpressionToken || nextKeyScalar is InsertExpressionToken && (m_context.Flags & Expressions2.ExpressionFlags.AllowAnyForInsert) != Expressions2.ExpressionFlags.None) {
                            var def = m_context.AutoCompleteMatches != null ? new DefinitionInfo(definition) : new DefinitionInfo(definition, "any");
                            if(m_context.AutoCompleteMatches != null && m_schema.Get<SequenceDefinition>(definition.Parent).Any()) {
                                var oneOf = new OneOfDefinition();
                                oneOf.OneOf.Add(definition.ParentName);
                                oneOf.OneOf.Add(definition.Name);
                                def.Definition = oneOf;
                            }
                            nextValue = ReadValue(def);
                        } else {
                            nextValue = ReadValue(m_context.AutoCompleteMatches != null ? new DefinitionInfo(definition) : anyDefinition);
                        }
                        mapping.Add(nextKeyScalar, nextValue);
                    }
                    // Illegal
                    else
                    {
                        m_context.Error(nextKeyScalar, TemplateStrings.ExpressionNotAllowed());
                        SkipValue();
                    }

                    continue;
                }

                // Not a string, convert
                if (!(nextKeyScalar is StringToken nextKey))
                {
                    nextKey = new StringToken(nextKeyScalar.FileId, nextKeyScalar.Line, nextKeyScalar.Column, nextKeyScalar.ToString());
                }

                // Duplicate
                if (!keys.Add(nextKey.Value))
                {
                    m_context.Error(nextKey, TemplateStrings.ValueAlreadyDefined(nextKey.Value));
                    SkipValue();
                    continue;
                }

                // Well known
                if (m_schema.TryMatchKey(mappingDefinitions, nextKey.Value, out String nextValueType, firstKey))
                {
                    m_memory.AddBytes(nextKey);
                    var nextValueDefinition = new DefinitionInfo(definition, nextValueType);
                    var last = m_context.AutoCompleteMatches?.LastOrDefault();
                    if(last != null && last.Token == nextKeyScalar) {
                        last.Description = nextValueDefinition.Definition.Description;
                    }
                    
                    var nextValue = ReadValue(nextValueDefinition);
                    mapping.Add(nextKey, nextValue);
                    continue;
                }

                // Loose
                if (looseKeyType != null)
                {
                    if (looseKeyDefinition == null)
                    {
                        looseKeyDefinition = new DefinitionInfo(definition, looseKeyType);
                        looseValueDefinition = new DefinitionInfo(definition, looseValueType);
                    }

                    Validate(nextKey, looseKeyDefinition.Value);
                    m_memory.AddBytes(nextKey);
                    var nextValue = ReadValue(looseValueDefinition.Value);
                    mapping.Add(nextKey, nextValue);
                    continue;
                }

                // Error
                m_context.Error(nextKey, TemplateStrings.UnexpectedValue(nextKey.Value));
                SkipValue();
            }

            if(m_context.AutoCompleteMatches != null) {
                var aentry = m_context.AutoCompleteMatches.Where(a => a.Token == mapping).FirstOrDefault();
                if(aentry != null) {
                    aentry.Definitions = mappingDefinitions.Cast<Definition>().ToArray();
                }
            }

            // Only one
            if (mappingDefinitions.Count > 1 && !hasExpressionKey)
            {
                var hitCount = new Dictionary<String, Int32>();
                foreach (MappingDefinition mapdef in mappingDefinitions)
                {
                    foreach (var kv in mapdef.Properties)
                    {
                        var key = kv.Key;
                        if (!hitCount.TryGetValue(key, out Int32 value))
                        {
                            hitCount.Add(key, 1);
                        }
                        else
                        {
                            hitCount[key] = value + 1;
                        }
                    }
                }

                List<String> nonDuplicates = new List<String>();
                foreach (String key in hitCount.Keys)
                {
                    if (hitCount[key] == 1)
                    {
                        nonDuplicates.Add(key);
                    }
                }
                nonDuplicates.Sort();

                String listToDeDuplicate = String.Join(", ", nonDuplicates);
                m_context.Error(mapping, TemplateStrings.UnableToDetermineOneOf(listToDeDuplicate));
            }
            else if (mappingDefinitions.Count == 1 && !hasExpressionKey)
            {
                foreach (var property in mappingDefinitions[0].Properties)
                {
                    if (property.Value.Required)
                    {
                        if (!keys.Contains(property.Key))
                        {
                            m_context.Error(mapping, $"Required property is missing: {property.Key}");
                        }
                    }
                }
            }
            ExpectMappingEnd(mapping);
        }

        private void HandleMappingWithAllLooseProperties(
            DefinitionInfo mappingDefinition,
            DefinitionInfo keyDefinition,
            DefinitionInfo valueDefinition,
            MappingToken mapping)
        {
            TemplateToken nextValue;
            var keys = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            while (m_objectReader.AllowLiteral(out LiteralToken rawLiteral))
            {
                var keyDef = new DefinitionInfo(mappingDefinition, "string");
                var nextKeyScalar = ParseScalar(rawLiteral, keyDef);
                var last = m_context.AutoCompleteMatches?.LastOrDefault();
                if(last != null && last.Token == nextKeyScalar) {
                    last.Description = valueDefinition.Definition.Description;
                }

                // Expression
                if (nextKeyScalar is ExpressionToken)
                {
                    // Legal
                    if (mappingDefinition.AllowedContext.Length > 0)
                    {
                        m_memory.AddBytes(nextKeyScalar);
                        if(nextKeyScalar is EachExpressionToken eachexp) {
                            var def = m_context.AutoCompleteMatches != null ? new DefinitionInfo(mappingDefinition) : new DefinitionInfo(mappingDefinition, "any");
                            def.AllowedContext = valueDefinition.AllowedContext.Append(eachexp.Variable).ToArray();
                            if(m_context.AutoCompleteMatches != null && m_schema.Get<SequenceDefinition>(mappingDefinition.Parent).Any()) {
                                var oneOf = new OneOfDefinition();
                                oneOf.OneOf.Add(mappingDefinition.ParentName);
                                oneOf.OneOf.Add(mappingDefinition.Name);
                                def.Definition = oneOf;
                            }
                            nextValue = ReadValue(def);
                        } else if(nextKeyScalar is ConditionalExpressionToken || nextKeyScalar is InsertExpressionToken && (m_context.Flags & Expressions2.ExpressionFlags.AllowAnyForInsert) != Expressions2.ExpressionFlags.None) {
                            var def = m_context.AutoCompleteMatches != null ? new DefinitionInfo(mappingDefinition) : new DefinitionInfo(mappingDefinition, "any");
                            if(m_context.AutoCompleteMatches != null && m_schema.Get<SequenceDefinition>(mappingDefinition.Parent).Any()) {
                                var oneOf = new OneOfDefinition();
                                oneOf.OneOf.Add(mappingDefinition.ParentName);
                                oneOf.OneOf.Add(mappingDefinition.Name);
                                def.Definition = oneOf;
                            }
                            nextValue = ReadValue(def);
                        } else {
                            nextValue = ReadValue(m_context.AutoCompleteMatches != null ? new DefinitionInfo(mappingDefinition) : valueDefinition);
                        }
                        mapping.Add(nextKeyScalar, nextValue);
                    }
                    // Illegal
                    else
                    {
                        m_context.Error(nextKeyScalar, TemplateStrings.ExpressionNotAllowed());
                        SkipValue();
                    }

                    continue;
                }

                // Not a string, convert
                if (!(nextKeyScalar is StringToken nextKey))
                {
                    nextKey = new StringToken(nextKeyScalar.FileId, nextKeyScalar.Line, nextKeyScalar.Column, nextKeyScalar.ToString());
                }

                // Duplicate
                if (!keys.Add(nextKey.Value))
                {
                    m_context.Error(nextKey, TemplateStrings.ValueAlreadyDefined(nextKey.Value));
                    SkipValue();
                    continue;
                }

                // Validate
                Validate(nextKey, keyDefinition);
                m_memory.AddBytes(nextKey);

                // Add the pair
                nextValue = ReadValue(valueDefinition);
                mapping.Add(nextKey, nextValue);
            }

            ExpectMappingEnd(mapping);
        }

        private void ExpectMappingEnd(MappingToken token)
        {
            if (!m_objectReader.AllowMappingEnd(token))
            {
                throw new Exception("Expected mapping end"); // Should never happen
            }
        }

        private void SkipValue(Boolean error = false)
        {
            m_memory.IncrementEvents();

            // Scalar
            if (m_objectReader.AllowLiteral(out LiteralToken literal))
            {
                if (error)
                {
                    m_context.Error(literal, TemplateStrings.UnexpectedValue(literal));
                }

                return;
            }

            // Sequence
            if (m_objectReader.AllowSequenceStart(out SequenceToken sequence))
            {
                m_memory.IncrementDepth();

                if (error)
                {
                    m_context.Error(sequence, TemplateStrings.UnexpectedSequenceStart());
                }

                while (!m_objectReader.AllowSequenceEnd())
                {
                    SkipValue();
                }

                m_memory.DecrementDepth();
                return;
            }

            // Mapping
            if (m_objectReader.AllowMappingStart(out MappingToken mapping))
            {
                m_memory.IncrementDepth();

                if (error)
                {
                    m_context.Error(mapping, TemplateStrings.UnexpectedMappingStart());
                }

                while (!m_objectReader.AllowMappingEnd())
                {
                    SkipValue();
                    SkipValue();
                }

                m_memory.DecrementDepth();
                return;
            }

            // Unexpected
            throw new InvalidOperationException(TemplateStrings.ExpectedScalarSequenceOrMapping());
        }

        private void Validate(
            StringToken stringToken,
            DefinitionInfo definition)
        {
            var scalar = stringToken as ScalarToken;
            Validate(ref scalar, definition);
        }

        private void Validate(
            ref ScalarToken scalar,
            DefinitionInfo definition)
        {
            switch (scalar.Type)
            {
                case TokenType.Null:
                case TokenType.Boolean:
                case TokenType.Number:
                case TokenType.String:
                    var literal = scalar as LiteralToken;

                    // Legal
                    if (definition.Get<ScalarDefinition>().Any(x => x.IsMatch(literal)))
                    {
                        return;
                    }

                    // Not a string, convert
                    if (literal.Type != TokenType.String)
                    {
                        literal = new StringToken(literal.FileId, literal.Line, literal.Column, literal.ToString());

                        // Legal
                        if (definition.Get<StringDefinition>().Any(x => x.IsMatch(literal)))
                        {
                            scalar = literal;
                            return;
                        }
                    }

                    // Illegal
                    m_context.Error(literal, TemplateStrings.UnexpectedValue(literal));
                    break;

                case TokenType.BasicExpression:

                    // Illegal
                    if (definition.AllowedContext.Length == 0)
                    {
                        m_context.Error(scalar, TemplateStrings.ExpressionNotAllowed());
                    }

                    break;

                default:
                    m_context.Error(scalar, TemplateStrings.UnexpectedValue(scalar));
                    break;
            }
        }

        private ScalarToken ParseScalar(
            LiteralToken token,
            DefinitionInfo definitionInfo)
        {
            AutoCompleteEntry completion = null;
            if(token is LiteralToken lit && (lit.PreWhiteSpace != null || lit.PostWhiteSpace != null || lit.RawData != null)) {
                completion = new AutoCompleteEntry {
                    Depth = m_memory.Depth,
                    Token = token,
                    AllowedContext = definitionInfo.AllowedContext,
                    Definitions = new [] { definitionInfo.Definition }
                };
                if(m_context.AutoCompleteMatches != null && Match(token) && (token.PostWhiteSpace == null || MatchPost(token) /*(m_context.Row < token.PostWhiteSpace.Line && !(token.PostWhiteSpace.Line == m_context.Row && token.PostWhiteSpace.Character > m_context.Column))*/)) {
                    var element = m_context.AutoCompleteMatches.FirstOrDefault(m => m.Depth == m_memory.Depth);
                    // Only Replace if the pre whitespace is beginning later
                    // Bug the next key scalar has been choosen
                    if(element?.Token?.PreWhiteSpace == null || completion?.Token?.PreWhiteSpace == null || element.Token.PreWhiteSpace.Line > completion.Token.PreWhiteSpace.Line || element.Token.PreWhiteSpace.Line == completion.Token.PreWhiteSpace.Line && element.Token.PreWhiteSpace.Character > completion.Token.PreWhiteSpace.Character ) {
                        m_context.AutoCompleteMatches.RemoveAll(m => m.Depth >= m_memory.Depth);
                        m_context.AutoCompleteMatches.Add(completion);
                    }
                } else {
                    completion.SemTokensOnly = true;
                }
                if(lit.RawData != null) {
#if HAVE_YAML_DOTNET_FORK
                    var praw = lit.RawData;

                    YamlDotNet.Core.Tokens.Scalar found = null;
                    var scanner = new YamlDotNet.Core.Scanner(new StringReader(praw), true);
                    try {
                        while(scanner.MoveNext() && !(scanner.Current is YamlDotNet.Core.Tokens.Error)) {
                            if(scanner.Current is YamlDotNet.Core.Tokens.Scalar s) {
                                found = s;
                                break;
                            }
                        }
                    } catch {

                    }

                    if(found != null) {
                        completion.Mapping = found.Mapping.Select(m => ((int)(m.Line - 1 + token.Line), m.Line == 1 ? (int)(token.Column - 1 + m.Column) : (int)m.Column)).ToArray();
                        completion.RMapping = found.RMapping.ToDictionary(m => ((int)(m.Key.Line - 1 + token.Line), (int)m.Key.Line == 1 ? (int)(token.Column - 1 + m.Key.Column) : (int)m.Key.Column), m => m.Value);
                    }
#else
                    var rand = new Random();
                    string C = "CX";
                    while(lit.RawData.Contains(C)) {
                        C = rand.Next(255).ToString("X2");
                    }
                    var praw = lit.ToString();
                    (int, int)[] mapping = new (int, int)[praw.Length + 1];
                    var rmapping = new Dictionary<(int, int), int>();
                    Array.Fill(mapping, (-1, -1));

                    int column = lit.Column.Value;
                    int line = lit.Line.Value;
                    int ridx = -1;
                    for(int idx = 0; idx < lit.RawData.Length; idx++) {
                        if(lit.RawData[idx] == '\n') {
                            line++;
                            column = 1;
                            continue;
                        }
                        var xraw = lit.RawData.Insert(idx, C);

                        var scanner = new YamlDotNet.Core.Scanner(new StringReader(xraw), true);
                        try {
                            while(scanner.MoveNext() && !(scanner.Current is YamlDotNet.Core.Tokens.Error)) {
                                if(scanner.Current is YamlDotNet.Core.Tokens.Scalar s) {
                                    var x = s.Value;
                                    var m = x.IndexOf(C);
                                    if(m >= 0 && m < mapping.Length && ridx <= m) {
                                        if(mapping[m] != (-1,-1)) {
                                            rmapping.Remove(mapping[m]);
                                        }
                                        mapping[m] = (line, column);
                                        rmapping[(line, column)] = m;
                                        ridx = m;
                                    }
                                }
                            }
                        } catch {

                        }
                        column++;
                    }
                    completion.Mapping = mapping;
                    completion.RMapping = rmapping;
#endif
                }
            }
            var allowedContext = definitionInfo.AllowedContext;
            var isExpression = definitionInfo.Definition is StringDefinition sdef && sdef.IsExpression;
            var actionsIfExpression = definitionInfo.Definition.ActionsIfExpression || isExpression;
            // Not a string
            if (token.Type != TokenType.String)
            {
                return token;
            }

            // Check if the value is definitely a literal
            var raw = token.ToString();
            Int32 startExpression;
            if (String.IsNullOrEmpty(raw) ||
                (startExpression = raw.IndexOf(TemplateConstants.OpenExpression)) < 0) // Doesn't contain ${{
            {
                if(!String.IsNullOrEmpty(raw) && isExpression) {
                    if(completion != null && completion.Index < 0) {
                        completion.Index = -1;
                    }
                    // Check if value should still be evaluated as an expression
                    var expression = ParseExpression(completion, token.Line, token.Column, raw, allowedContext, out Exception ex);
                    // Check for error
                    if (ex != null) {
                        m_context.Error(token, ex);
                    } else {
                        return expression;
                    }
                }
                return token;
            }

            // Break the value into segments of LiteralToken and ExpressionToken
            var segments = new List<ScalarToken>();
            var i = 0;
            while (i < raw.Length)
            {
                // An expression starts here:
                if (i == startExpression)
                {
                    if(completion?.Mapping?.Length > startExpression && token.Line != null && token.Column != null) {
                        var (r, c) = completion.Mapping[startExpression];
                        m_context.AddSemToken(r, c, 3, 5, 0);
                    }

                    // Find the end of the expression - i.e. }}
                    startExpression = i;
                    var endExpression = -1;
                    var inString = false;
                    for (i += TemplateConstants.OpenExpression.Length; i < raw.Length; i++)
                    {
                        if (raw[i] == '\'')
                        {
                            inString = !inString; // Note, this handles escaped single quotes gracefully. Ex. 'foo''bar'
                        }
                        else if (!inString && raw[i] == '}' && raw[i - 1] == '}')
                        {
                            endExpression = i;
                            i++;
                            break;
                        }
                    }

                    bool hasEnd = false;

                    // Check if not closed
                    if (endExpression < startExpression)
                    {
                        m_context.Error(token, TemplateStrings.ExpressionNotClosed());
                        if(completion == null) {
                            return token;
                        }
                        endExpression = raw.Length + TemplateConstants.CloseExpression.Length - 1;
                    } else {
                        hasEnd = true;
                    }

                    if(completion != null && completion.Index < 0) {
                        completion.Index = - (startExpression + TemplateConstants.OpenExpression.Length + 1);
                    }

                    // Parse the expression
                    var rawExpression = raw.Substring(
                        startExpression + TemplateConstants.OpenExpression.Length,
                        endExpression - startExpression + 1 - TemplateConstants.OpenExpression.Length - TemplateConstants.CloseExpression.Length);
                    var expression = ParseExpression(completion, token.Line, token.Column, rawExpression, allowedContext, out Exception ex);

                    if(completion?.Mapping?.Length >= endExpression - 1 && hasEnd && token.Line != null && token.Column != null) {
                        var (r, c) = completion.Mapping[endExpression - 1];
                        m_context.AddSemToken(r, c, 2, 5, 0);
                    }

                    // Check for error
                    if (ex != null)
                    {
                        m_context.Error(token, ex);
                        return token;
                    }

                    // Check if a directive was used when not allowed
                    if (!String.IsNullOrEmpty(expression.Directive) &&
                        ((startExpression != 0) || (i < raw.Length)))
                    {
                        m_context.Error(token, TemplateStrings.DirectiveNotAllowedInline(expression.Directive));
                        return token;
                    }

                    // Add the segment
                    segments.Add(expression);

                    // Look for the next expression
                    startExpression = raw.IndexOf(TemplateConstants.OpenExpression, i);
                }
                // The next expression is further ahead:
                else if (i < startExpression)
                {
                    // Append the segment
                    AddString(segments, token.Line, token.Column, raw.Substring(i, startExpression - i));

                    // Adjust the position
                    i = startExpression;
                }
                // No remaining expressions:
                else
                {
                    AddString(segments, token.Line, token.Column, raw.Substring(i));
                    break;
                }
            }

            // Check if can convert to a literal
            // For example, the escaped expression: ${{ '{{ this is a literal }}' }}
            if (segments.Count == 1 &&
                segments[0] is BasicExpressionToken basicExpression &&
                IsExpressionString(basicExpression.Expression, out String str))
            {
                return new StringToken(m_fileId, token.Line, token.Column, str);
            }

            // Check if only ony segment
            if (segments.Count == 1)
            {
                return segments[0];
            }

            if (actionsIfExpression && (m_context.Flags & Expressions2.ExpressionFlags.FailInvalidActionsIfExpression) != Expressions2.ExpressionFlags.None)
            {
                m_context.Error(token, $"If condition has been converted to format expression and won't evaluate correctly: {raw}");
            }
            if (actionsIfExpression && (m_context.Flags & Expressions2.ExpressionFlags.FixInvalidActionsIfExpression) != Expressions2.ExpressionFlags.None)
            {
                var fixedExpression = new StringBuilder();
                foreach (var segment in segments)
                {
                    if (segment is StringToken literal)
                    {
                        fixedExpression.Append(literal.Value);
                    }
                    else
                    {
                        fixedExpression.Append((segment as BasicExpressionToken).Expression);
                    }
                }
                return new BasicExpressionToken(m_fileId, token.Line, token.Column, fixedExpression.ToString());
            }

            // Build the new expression, using the format function
            var format = new StringBuilder();
            var args = new StringBuilder();
            var argIndex = 0;
            foreach (var segment in segments)
            {
                if (segment is StringToken literal)
                {
                    var text = ExpressionUtility.StringEscape(literal.Value) // Escape quotes
                        .Replace("{", "{{") // Escape braces
                        .Replace("}", "}}");
                    format.Append(text);
                }
                else
                {
                    format.Append("{" + argIndex.ToString(CultureInfo.InvariantCulture) + "}"); // Append formatter
                    argIndex++;

                    var expression = segment as BasicExpressionToken;
                    args.Append(", ");
                    args.Append(expression.Expression);
                }
            }

            return new BasicExpressionToken(m_fileId, token.Line, token.Column, $"format('{format}'{args})");
        }

        private bool AutoCompleteExpression(AutoCompleteEntry completion, int offset, string value, int poffset = 0) {
            if(completion != null)
            {
                LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer(value.Substring(offset), m_context.Flags);
                Token tkn = null;
                var startIndex = -1 - completion.Index + offset;
                var lit = completion.Token as LiteralToken;
                if(lit.RawData != null) {
                    var mapping = completion.Mapping;
                    while(lexicalAnalyzer.TryGetNextToken(ref tkn)) {
                        var (r, c) = mapping[startIndex + tkn.Index];
                        if(tkn.Kind == TokenKind.Function) {
                            m_context.AddSemToken(r, c, tkn.RawValue.Length, 2, 2);
                        } else if(tkn.Kind == TokenKind.NamedValue) {
                            m_context.AddSemToken(r, c, tkn.RawValue.Length, 0, 1);
                        } else if(tkn.Kind == TokenKind.PropertyName) {
                            m_context.AddSemToken(r, c, tkn.RawValue.Length, 3, 0);
                        } else if(tkn.Kind == TokenKind.Boolean || tkn.Kind == TokenKind.Null) {
                            m_context.AddSemToken(r, c, tkn.RawValue.Length, 4, 2);
                        } else if(tkn.Kind == TokenKind.Number || tkn.Kind == TokenKind.String && tkn.ParsedValue is VersionWrapper) {
                            m_context.AddSemToken(r, c, tkn.RawValue.Length, 4, 4);
                        } else if(tkn.Kind == TokenKind.StartGroup || tkn.Kind == TokenKind.StartIndex || tkn.Kind == TokenKind.StartParameters || tkn.Kind == TokenKind.EndGroup || tkn.Kind == TokenKind.EndParameters
                            || tkn.Kind == TokenKind.EndIndex || tkn.Kind == TokenKind.Wildcard || tkn.Kind == TokenKind.Separator || tkn.Kind == TokenKind.LogicalOperator || tkn.Kind == TokenKind.Dereference) {
                            m_context.AddSemToken(r, c, tkn.RawValue.Length, 5, 0);
                        } else if(tkn.Kind == TokenKind.String) {
                            var (er, ec) = mapping[startIndex + tkn.Index + tkn.RawValue.Length];
                            // Only add single line string
                            if(er == r && c < ec) {
                                m_context.AddSemToken(r, c, ec - c /* May contain escape codes */, 6, 0);
                            }
                            // TODO multi line string by splitting them
                        }
                    }
                }
            }
            if(completion != null && !completion.SemTokensOnly && m_context.AutoCompleteMatches != null) {
                // var idx = GetIdxOfExpression(completion.Token as LiteralToken, m_context.Row.Value, m_context.Column.Value);
                var idx = completion.RMapping.TryGetValue((m_context.Row.Value, m_context.Column.Value), out var i) ? i : -1;
                var startIndex = -1 - completion.Index + offset;
                if(idx != -1 && idx >= startIndex && (idx <= startIndex + value.Length + poffset)) {
                    LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer(value.Substring(offset), m_context.Flags);
                    Token tkn = null;
                    List<Token> tkns = new List<Token>();
                    while(lexicalAnalyzer.TryGetNextToken(ref tkn)) {
                        if(tkn.Index + startIndex > idx) {
                            break;
                        }
                        tkns.Add(tkn);
                    }
                    completion.Tokens = tkns;
                    completion.Index = idx - startIndex;
                }
            }
            return true;
        }

        private ExpressionToken ParseExpression(
            AutoCompleteEntry completion,
            Int32? line,
            Int32? column,
            String value,
            String[] allowedContext,
            out Exception ex)
        {
            // TODO !!!!! If the expressions parameter is missing in directives provide auto completion
            // It's buggy
            // Empty expressions like ${{  }} are not auto completed?
            var trimmed = value.Trim();

            // Check if the value is empty
            if (String.IsNullOrEmpty(trimmed))
            {
                AutoCompleteExpression(completion, 0, value);
                ex = new ArgumentException(TemplateStrings.ExpectedExpression());
                return null;
            }
            var trimmedNo = value.IndexOf(trimmed);

            bool extendedDirectives = (m_context.Flags & Expressions2.ExpressionFlags.ExtendedDirectives) != Expressions2.ExpressionFlags.None;
            // Try to find a matching directive
            List<(int, String)> parameters;
            if (MatchesDirective(trimmed, TemplateConstants.InsertDirective, 0, out parameters, out ex))
            {
                return new InsertExpressionToken(m_fileId, line, column);
            }
            else if (ex != null)
            {
                return null;
            }
            else if (extendedDirectives && MatchesDirective(trimmed, "if", 1, out parameters, out ex) && AutoCompleteExpression(completion, trimmedNo + parameters[0].Item1, value) && ExpressionToken.IsValidExpression(parameters[0].Item2, allowedContext, out ex, m_context.Flags) || parameters?.Count == 1 && !AutoCompleteExpression(completion, trimmedNo + parameters[0].Item1, value))
            {
                return new IfExpressionToken(m_fileId, line, column, parameters[0].Item2);
            }
            else if (ex != null)
            {
                return null;
            }
            else if (extendedDirectives && MatchesDirective(trimmed, "elseif", 1, out parameters, out ex) && AutoCompleteExpression(completion, trimmedNo + parameters[0].Item1, value) && ExpressionToken.IsValidExpression(parameters[0].Item2, allowedContext, out ex, m_context.Flags) || parameters?.Count == 1 && !AutoCompleteExpression(completion, trimmedNo + parameters[0].Item1, value))
            {
                return new ElseIfExpressionToken(m_fileId, line, column, parameters[0].Item2);
            }
            else if (ex != null)
            {
                return null;
            }
            else if (extendedDirectives && MatchesDirective(trimmed, "else", 0, out parameters, out ex))
            {
                return new ElseExpressionToken(m_fileId, line, column);
            }
            else if (ex != null)
            {
                return null;
            }
            else if (extendedDirectives && MatchesDirective(trimmed, "each", 3, out parameters, out ex) && parameters[1].Item2 == "in" && AutoCompleteExpression(completion, trimmedNo + parameters[2].Item1, value) && ExpressionToken.IsValidExpression(parameters[2].Item2, allowedContext, out ex, m_context.Flags) || parameters?.Count == 3 && !AutoCompleteExpression(completion, trimmedNo + parameters[2].Item1, value))
            {
                return new EachExpressionToken(m_fileId, line, column, parameters[0].Item2, parameters[2].Item2);
            }
            else if (ex != null)
            {
                return null;
            }

            AutoCompleteExpression(completion, 0, value);

            // Check if the value is an expression
            if (!ExpressionToken.IsValidExpression(trimmed, allowedContext, out ex, m_context.Flags))
            {
                return null;
            }

            // Return the expression
            return new BasicExpressionToken(m_fileId, line, column, trimmed);
        }

        private void AddString(
            List<ScalarToken> segments,
            Int32? line,
            Int32? column,
            String value)
        {
            // If the last segment was a LiteralToken, then append to the last segment
            if (segments.Count > 0 && segments[segments.Count - 1] is StringToken lastSegment)
            {
                segments[segments.Count - 1] = new StringToken(m_fileId, line, column, lastSegment.Value + value);
            }
            // Otherwise add a new LiteralToken
            else
            {
                segments.Add(new StringToken(m_fileId, line, column, value));
            }
        }

        private static Boolean MatchesDirective(
            String trimmed,
            String directive,
            Int32 expectedParameters,
            out List<(int, String)> parameters,
            out Exception ex)
        {
            if (trimmed.StartsWith(directive, StringComparison.Ordinal) &&
                (trimmed.Length == directive.Length || Char.IsWhiteSpace(trimmed[directive.Length])))
            {
                parameters = new List<(int, String)>();
                var startIndex = directive.Length;
                var inString = false;
                var parens = 0;
                for (var i = startIndex; i < trimmed.Length; i++)
                {
                    var c = trimmed[i];
                    if (Char.IsWhiteSpace(c) && !inString && parens == 0)
                    {
                        if (startIndex < i)
                        {
                            parameters.Add((startIndex, trimmed.Substring(startIndex, i - startIndex)));
                        }

                        startIndex = i + 1;
                    }
                    else if (c == '\'')
                    {
                        inString = !inString;
                    }
                    else if (c == '(' && !inString)
                    {
                        parens++;
                    }
                    else if (c == ')' && !inString)
                    {
                        parens--;
                    }
                }

                if (startIndex < trimmed.Length)
                {
                    parameters.Add((startIndex, trimmed.Substring(startIndex)));
                }

                if (expectedParameters != parameters.Count)
                {
                    ex = new ArgumentException(TemplateStrings.ExpectedNParametersFollowingDirective(expectedParameters, directive, parameters.Count));
                    if(expectedParameters == parameters.Count + 1) {
                        parameters.Add((parameters.LastOrDefault().Item1 + (parameters.LastOrDefault().Item2?.Length ?? 2) + 1, ""));
                    } else {
                        parameters = null;
                    }
                    return false;
                }

                ex = null;
                return true;
            }

            ex = null;
            parameters = null;
            return false;
        }

        private static Boolean IsExpressionString(
            String trimmed,
            out String str)
        {
            var builder = new StringBuilder();

            var inString = false;
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (c == '\'')
                {
                    inString = !inString;

                    if (inString && i != 0)
                    {
                        builder.Append(c);
                    }
                }
                else if (!inString)
                {
                    str = default;
                    return false;
                }
                else
                {
                    builder.Append(c);
                }
            }

            str = builder.ToString();
            return true;
        }

        private struct DefinitionInfo
        {
            public DefinitionInfo(
                TemplateSchema schema,
                String name)
            {
                m_schema = schema;

                Parent = null;
                ParentName = null;
                Name = name;

                // Lookup the definition
                Definition = m_schema.GetDefinition(name);

                // Record allowed context
                AllowedContext = Definition.ReaderContext;
            }

            public DefinitionInfo(
                DefinitionInfo parent)
            {
                m_schema = parent.m_schema;
                Parent = parent.Definition;
                ParentName = parent.ParentName;

                Name = parent.Name;

                // Lookup the definition
                Definition = parent.Definition;

                // Record allowed context
                AllowedContext = parent.AllowedContext.ToArray();
            }

            public DefinitionInfo(
                DefinitionInfo parent,
                String name)
            {
                m_schema = parent.m_schema;

                Parent = parent.Definition;
                ParentName = parent.Name;

                Name = name;

                // Lookup the definition
                Definition = m_schema.GetDefinition(name);

                // Record allowed context
                if (Definition.ReaderContext.Length > 0)
                {
                    AllowedContext = new HashSet<String>(parent.AllowedContext.Concat(Definition.ReaderContext), StringComparer.OrdinalIgnoreCase).ToArray();
                }
                else
                {
                    AllowedContext = parent.AllowedContext;
                }
            }

            public IEnumerable<T> Get<T>()
                where T : Definition
            {
                return m_schema.Get<T>(Definition);
            }

            public Definition? Parent { get; }
            public string? ParentName { get; }

            private TemplateSchema m_schema;
            public string? Name { get; }
            public Definition Definition;
            public String[] AllowedContext;
        }

        private readonly TemplateContext m_context;
        private readonly Int32? m_fileId;
        private readonly TemplateMemory m_memory;
        private readonly IObjectReader m_objectReader;
        private readonly TemplateSchema m_schema;
    }
}
