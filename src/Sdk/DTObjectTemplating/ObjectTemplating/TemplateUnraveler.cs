using System;
using System.Collections.Generic;
using System.Text;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// This class allows callers to easily traverse a template object.
    /// This class hides the details of expression expansion, depth tracking,
    /// and memory tracking.
    /// </summary>
    internal sealed class TemplateUnraveler
    {
        internal TemplateUnraveler(
            TemplateContext context,
            TemplateToken template,
            Int32 removeBytes)
        {
            m_context = context;
            m_memory = context.Memory;

            // Initialize the reader state
            MoveFirst(template, removeBytes);
        }

        internal Boolean AllowScalar(
            Boolean expand,
            out ScalarToken scalar)
        {
            m_memory.IncrementEvents();

            if (expand)
            {
                Unravel(expand: true);
            }

            if (m_current?.Value is ScalarToken scalarToken)
            {
                scalar = scalarToken;

                // Add bytes before they are emitted to the caller (so the caller doesn't have to track bytes)
                m_memory.AddBytes(scalar);

                MoveNext();
                return true;
            }

            scalar = null;
            return false;
        }

        internal Boolean AllowSequenceStart(
            Boolean expand,
            out SequenceToken sequence)
        {
            m_memory.IncrementEvents();

            if (expand)
            {
                Unravel(expand: true);
            }

            if (m_current is SequenceState sequenceState && sequenceState.IsStart)
            {
                sequence = new SequenceToken(sequenceState.Value.FileId, sequenceState.Value.Line, sequenceState.Value.Column);

                // Add bytes before they are emitted to the caller (so the caller doesn't have to track bytes)
                m_memory.AddBytes(sequence);

                MoveNext();
                return true;
            }

            sequence = null;
            return false;
        }

        internal Boolean AllowSequenceEnd(Boolean expand)
        {
            m_memory.IncrementEvents();

            if (expand)
            {
                Unravel(expand: true);
            }

            if (m_current is SequenceState sequenceState && sequenceState.IsEnd)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        internal Boolean AllowMappingStart(
            Boolean expand,
            out MappingToken mapping)
        {
            m_memory.IncrementEvents();

            if (expand)
            {
                Unravel(expand: true);
            }

            if (m_current is MappingState mappingState && mappingState.IsStart)
            {
                mapping = new MappingToken(mappingState.Value.FileId, mappingState.Value.Line, mappingState.Value.Column);

                // Add bytes before they are emitted to the caller (so the caller doesn't have to track bytes)
                m_memory.AddBytes(mapping);

                MoveNext();
                return true;
            }

            mapping = null;
            return false;
        }

        internal Boolean AllowMappingEnd(Boolean expand)
        {
            m_memory.IncrementEvents();

            if (expand)
            {
                Unravel(expand: true);
            }

            if (m_current is MappingState mappingState && mappingState.IsEnd)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        internal void ReadEnd()
        {
            m_memory.IncrementEvents();

            if (m_current != null)
            {
                throw new InvalidOperationException("Expected end of template object. " + DumpState());
            }
        }

        internal void ReadMappingEnd()
        {
            if (!AllowMappingEnd(expand: false))
            {
                throw new InvalidOperationException("Unexpected state while attempting to read the mapping end. " + DumpState());
            }
        }

        internal void SkipSequenceItem()
        {
            m_memory.IncrementEvents();

            if (!(m_current?.Parent is SequenceState ancestor))
            {
                throw new InvalidOperationException("Unexpected state while attempting to skip the current sequence item. " + DumpState());
            }

            MoveNext(skipNestedEvents: true);
        }

        internal void SkipMappingKey()
        {
            m_memory.IncrementEvents();

            if (!(m_current?.Parent is MappingState ancestor) || !ancestor.IsKey)
            {
                throw new InvalidOperationException("Unexpected state while attempting to skip the current mapping key. " + DumpState());
            }

            MoveNext(skipNestedEvents: true);
        }

        internal void SkipMappingValue()
        {
            m_memory.IncrementEvents();

            if (!(m_current?.Parent is MappingState ancestor) || ancestor.IsKey)
            {
                throw new InvalidOperationException("Unexpected state while attempting to skip the current mapping value. " + DumpState());
            }

            MoveNext(skipNestedEvents: true);
        }

        private String DumpState()
        {
            var result = new StringBuilder();

            if (m_current == null)
            {
                result.AppendLine("State: (null)");
            }
            else
            {
                result.AppendLine("State:");
                result.AppendLine();

                // Push state hierarchy
                var stack = new Stack<ReaderState>();
                var curr = m_current;
                while (curr != null)
                {
                    result.AppendLine(curr.ToString());
                    curr = curr.Parent;
                }
            }

            return result.ToString();
        }

        private void MoveFirst(
            TemplateToken value,
            Int32 removeBytes)
        {
            if (!(value is LiteralToken) && !(value is SequenceToken) && !(value is MappingToken) && !(value is BasicExpressionToken))
            {
                throw new NotSupportedException($"Unexpected type '{value?.GetType().Name}' when initializing object reader state");
            }

            m_memory.IncrementEvents();
            m_current = ReaderState.CreateState(null, value, m_context, removeBytes);
        }

        private void MoveNext(Boolean skipNestedEvents = false)
        {
            m_memory.IncrementEvents();

            if (m_current == null)
            {
                return;
            }

            // Sequence start
            if (m_current is SequenceState sequenceState &&
                sequenceState.IsStart &&
                !skipNestedEvents)
            {
                // Move to the first item or sequence end
                m_current = sequenceState.Next();
            }
            // Mapping start
            else if (m_current is MappingState mappingState &&
                mappingState.IsStart &&
                !skipNestedEvents)
            {
                // Move to the first item key or mapping end
                m_current = mappingState.Next();
            }
            // Parent is a sequence
            else if (m_current.Parent is SequenceState parentSequenceState)
            {
                // Move to the next item or sequence end
                m_current.Remove();
                m_current = parentSequenceState.Next();
            }
            // Parent is a mapping
            else if (m_current.Parent is MappingState parentMappingState)
            {
                // Move to the next item value, item key, or mapping end
                m_current.Remove();
                m_current = parentMappingState.Next();
            }
            // Parent is an expression end
            else if (m_current.Parent != null)
            {
                m_current.Remove();
                m_current = m_current.Parent;
            }
            // Parent is null
            else
            {
                m_current.Remove();
                m_current = null;
            }

            m_expanded = false;
            Unravel(expand: false);
        }

        private void Unravel(Boolean expand)
        {
            if (m_expanded)
            {
                return;
            }

            do
            {
                if (m_current == null)
                {
                    break;
                }
                // Literal
                else if (m_current is LiteralState literalState)
                {
                    break;
                }
                else if (m_current is BasicExpressionState basicExpressionState)
                {
                    // Sequence item is a basic expression start
                    // For example:
                    //   steps:
                    //   - script: credScan
                    //   - ${{ parameters.preBuild }}
                    //   - script: build
                    if (basicExpressionState.IsStart &&
                        m_current.Parent is SequenceState)
                    {
                        if (expand)
                        {
                            SequenceItemBasicExpression();
                        }
                        else
                        {
                            break;
                        }
                    }
                    // Mapping key is a basic expression start
                    // For example:
                    //   steps:
                    //   - ${{ parameters.scriptHost }}: echo hi
                    else if (basicExpressionState.IsStart &&
                        m_current.Parent is MappingState parentMappingState &&
                        parentMappingState.IsKey)
                    {
                        if (expand)
                        {
                            MappingKeyBasicExpression();
                        }
                        else
                        {
                            break;
                        }
                    }
                    // Mapping value is a basic expression start
                    // For example:
                    //   steps:
                    //   - script: credScan
                    //   - script: ${{ parameters.tool }}
                    else if (basicExpressionState.IsStart &&
                        m_current.Parent is MappingState parentMappingState2 &&
                        !parentMappingState2.IsKey)
                    {
                        if (expand)
                        {
                            MappingValueBasicExpression();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (basicExpressionState.IsStart &&
                        m_current.Parent is null)
                    {
                        if (expand)
                        {
                            RootBasicExpression();
                        }
                        else
                        {
                            break;
                        }
                    }
                    // Basic expression end
                    else if (basicExpressionState.IsEnd)
                    {
                        EndExpression();
                    }
                    else
                    {
                        UnexpectedState();
                    }
                }
                else if (m_current is MappingState mappingState)
                {
                    // Mapping end, closing an "insert" mapping insertion
                    if (mappingState.IsEnd &&
                        (m_current.Parent is InsertExpressionState || m_current.Parent is ConditionalExpressionState) )
                    {
                        m_current.Remove();
                        m_current = m_current.Parent; // Skip to the expression end
                    } else if (mappingState.IsEnd &&
                        m_current.Parent is EachExpressionState eachexpr )
                    {
                        m_current.Remove();
                        if(eachexpr.IsEnd) {
                            m_current = m_current.Parent; // Skip to the expression end
                        } else {
                            m_current = eachexpr.InsertMoveNext(m_context);
                        }
                    }
                    // Normal mapping start
                    else if (mappingState.IsStart)
                    {
                        if(expand && mappingState.Parent is SequenceState && mappingState.Value.Count == 1 && (mappingState.Value[0].Key.Type == TokenType.IfExpression || mappingState.Value[0].Key.Type == TokenType.ElseExpression || mappingState.Value[0].Key.Type == TokenType.ElseIfExpression) && mappingState.Value[0].Value.Type == TokenType.Sequence ) {
                            mappingState.Remove();
                            m_current = ReaderState.CreateState(mappingState.Parent, mappingState.Value[0].Key, m_context);
                        } else if(expand && mappingState.Parent is SequenceState && mappingState.Value.Count == 1 && mappingState.Value[0].Key.Type == TokenType.EachExpression && mappingState.Value[0].Value.Type == TokenType.Sequence) {
                            mappingState.Remove();
                            m_current = ReaderState.CreateState(mappingState.Parent, mappingState.Value[0].Key, m_context);
                            var eachExpressionState = m_current as EachExpressionState;
                            eachExpressionState.Content = mappingState.Value[0].Value;
                            if(m_context.ExpressionValues.ContainsKey(eachExpressionState.Value.Variable)) {
                                m_context.Error(eachExpressionState.Value, TemplateStrings.ValueAlreadyDefined(eachExpressionState.Value.Variable));
                                m_current.Remove();
                                m_current = eachExpressionState.ToStringToken();
                                // throw new ArgumentException($"The idenfifier '{eachExpressionState.Value.Variable}' has already been defined within the current scope");
                            } else {
                                eachExpressionState.Sequence = new BasicExpressionToken(null, null, null, eachExpressionState.Value.Collection).EvaluateTemplateToken(m_context, out _).AssertSequence("seq");
                                if(!eachExpressionState.IsEnd) {
                                    m_current = eachExpressionState.MoveNext(m_context);
                                }
                            }
                        } else {
                            break;
                        }
                    }
                    // Normal mapping end
                    else if (mappingState.IsEnd)
                    {
                        break;
                    }
                    else
                    {
                        UnexpectedState();
                    }
                }
                else if (m_current is SequenceState sequenceState)
                {
                    // Sequence end, closing a sequence insertion
                    if (sequenceState.IsEnd &&
                        m_current.Parent is BasicExpressionState &&
                        m_current.Parent.Parent is SequenceState)
                    {
                        m_current.Remove();
                        m_current = m_current.Parent; // Skip to the expression end
                    }
                    else if (sequenceState.IsEnd &&
                        m_current.Parent is ConditionalExpressionState)
                    {
                        m_current.Remove();
                        m_current = m_current.Parent; // Skip to the expression end
                    }
                    else if (sequenceState.IsEnd &&
                        m_current.Parent is EachExpressionState eachExpressionState)
                    {
                        m_current.Remove();
                        if(eachExpressionState.IsEnd) {
                            m_current = m_current.Parent; // Skip to the expression end
                            m_context.ExpressionValues.Remove(eachExpressionState.Value.Variable);
                        } else {
                            m_current = eachExpressionState.MoveNext(m_context);
                        }
                    }
                    // Normal sequence start
                    else if (sequenceState.IsStart)
                    {
                        break;
                    }
                    // Normal sequence end
                    else if (sequenceState.IsEnd)
                    {
                        break;
                    }
                    else
                    {
                        UnexpectedState();
                    }
                }
                else if (m_current is InsertExpressionState insertExpressionState)
                {
                    // Mapping key, beginning an "insert" mapping insertion
                    // For example:
                    //   - job: a
                    //     variables:
                    //       ${{ insert }}: ${{ parameters.jobVariables }}
                    if (insertExpressionState.IsStart &&
                        m_current.Parent is MappingState parentMappingState &&
                        parentMappingState.IsKey)
                    {
                        if (expand)
                        {
                            StartMappingInsertion();
                        }
                        else
                        {
                            break;
                        }
                    }
                    // Expression end
                    else if (insertExpressionState.IsEnd)
                    {
                        EndExpression();
                    }
                    // Not allowed
                    else if (insertExpressionState.IsStart)
                    {
                        m_context.Error(insertExpressionState.Value, TemplateStrings.DirectiveNotAllowed(insertExpressionState.Value.Directive));
                        m_current.Remove();
                        m_current = insertExpressionState.ToStringToken();
                    }
                    else
                    {
                        UnexpectedState();
                    }
                }
                else if (m_current is ConditionalExpressionState conditionalExpressionState)
                {
                    // Mapping key, beginning an "insert" mapping insertion
                    // For example:
                    //   - job: a
                    //     variables:
                    //       ${{ insert }}: ${{ parameters.jobVariables }}
                    if (conditionalExpressionState.IsStart &&
                        ((m_current.Parent is MappingState parentMappingState &&
                        parentMappingState.IsKey) || 
                        m_current.Parent is SequenceState))
                    {
                        if (expand)
                        {
                            StartIfInsertion();
                        }
                        else
                        {
                            break;
                        }
                    }
                    // Expression end
                    else if (conditionalExpressionState.IsEnd)
                    {
                        EndExpression();
                    }
                    // Not allowed
                    else if (conditionalExpressionState.IsStart)
                    {
                        m_context.Error(conditionalExpressionState.Value, TemplateStrings.DirectiveNotAllowed(conditionalExpressionState.Value.Directive));
                        m_current.Remove();
                        m_current = conditionalExpressionState.ToStringToken();
                    }
                    else
                    {
                        UnexpectedState();
                    }
                }
                else if (m_current is EachExpressionState eachExpressionState)
                {
                    // Mapping key, beginning an "each" mapping insertion
                    // For example:
                    //   - job: a
                    //     variables:
                    //       ${{ insert }}: ${{ parameters.jobVariables }}
                    if (!eachExpressionState.IsEnd &&
                        m_current.Parent is MappingState parentMappingState &&
                        parentMappingState.IsKey)
                    {
                        if (expand)
                        {
                            eachExpressionState.Sequence = new BasicExpressionToken(null, null, null, eachExpressionState.Value.Collection).EvaluateTemplateToken(m_context, out _).AssertSequence("seq");
                            if(!eachExpressionState.IsEnd) {
                                m_current = eachExpressionState.InsertMoveNext(m_context);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    // Expression end
                    else if (eachExpressionState.IsEnd)
                    {
                        EndExpression();
                    }
                    // Not allowed
                    else if (eachExpressionState.IsStart)
                    {
                        m_context.Error(eachExpressionState.Value, TemplateStrings.DirectiveNotAllowed(eachExpressionState.Value.Directive));
                        m_current.Remove();
                        m_current = eachExpressionState.ToStringToken();
                    }
                    else
                    {
                        UnexpectedState();
                    }
                }
                else
                {
                    UnexpectedState();
                }

                m_memory.IncrementEvents();
            } while (true);

            m_expanded = expand;
        }

        private void SequenceItemBasicExpression()
        {
            // The template looks like:
            //
            //   steps:
            //   - ${{ parameters.preSteps }}
            //   - script: build
            //
            // The current state looks like:
            //
            //   MappingState   // The document starts with a mapping
            //
            //   SequenceState  // The "steps" sequence
            //
            //   BasicExpressionState   // m_current

            var expressionState = m_current as BasicExpressionState;
            var expression = expressionState.Value;
            TemplateToken value;
            var removeBytes = 0;
            try
            {
                value = expression.EvaluateTemplateToken(expressionState.Context, out removeBytes);
            }
            catch (Exception ex)
            {
                m_context.Error(expression, ex);
                value = null;
            }

            // Move to the nested sequence, skip the sequence start
            if (value is SequenceToken nestedSequence)
            {
                m_current = expressionState.Next(nestedSequence, isSequenceInsertion: true, removeBytes: removeBytes);
            }
            // Move to the new value
            else if (value != null)
            {
                m_current = expressionState.Next(value, removeBytes);
            }
            // Move to the expression end
            else if (value == null)
            {
                expressionState.End();
            }
        }

        private void MappingKeyBasicExpression()
        {
            // The template looks like:
            //
            //   steps:
            //   - ${{ parameters.scriptHost }}: echo hi
            //
            // The current state looks like:
            //
            //   MappingState   // The document starts with a mapping
            //
            //   SequenceState  // The "steps" sequence
            //
            //   MappingState   // The step mapping
            //
            //   BasicExpressionState   // m_current

            // The expression should evaluate to a string
            var expressionState = m_current as BasicExpressionState;
            var expression = expressionState.Value as BasicExpressionToken;
            StringToken stringToken;
            var removeBytes = 0;
            try
            {
                stringToken = expression.EvaluateStringToken(expressionState.Context, out removeBytes);
            }
            catch (Exception ex)
            {
                m_context.Error(expression, ex);
                stringToken = null;
            }

            // Move to the stringToken
            if (stringToken != null)
            {
                m_current = expressionState.Next(stringToken, removeBytes);
            }
            // Move to the next key or mapping end
            else
            {
                m_current.Remove();
                var parentMappingState = m_current.Parent as MappingState;
                parentMappingState.Next().Remove(); // Skip the value
                m_current = parentMappingState.Next(); // Next key or mapping end
            }
        }

        private void MappingValueBasicExpression()
        {
            // The template looks like:
            //
            //   steps:
            //   - script: credScan
            //   - script: ${{ parameters.tool }}
            //
            // The current state looks like:
            //
            //   MappingState   // The document starts with a mapping
            //
            //   SequenceState  // The "steps" sequence
            //
            //   MappingState   // The step mapping
            //
            //   BasicExpressionState   // m_current

            var expressionState = m_current as BasicExpressionState;
            var expression = expressionState.Value;
            TemplateToken value;
            var removeBytes = 0;
            try
            {
                value = expression.EvaluateTemplateToken(expressionState.Context, out removeBytes);
            }
            catch (Exception ex)
            {
                m_context.Error(expression, ex);
                value = new StringToken(expression.FileId, expression.Line, expression.Column, String.Empty);
            }

            // Move to the new value
            m_current = expressionState.Next(value, removeBytes);
        }

        private void RootBasicExpression()
        {
            // The template looks like:
            //
            //   ${{ parameters.tool }}
            //
            // The current state looks like:
            //
            //   BasicExpressionState   // m_current

            var expressionState = m_current as BasicExpressionState;
            var expression = expressionState.Value;
            TemplateToken value;
            var removeBytes = 0;
            try
            {
                value = expression.EvaluateTemplateToken(expressionState.Context, out removeBytes);
            }
            catch (Exception ex)
            {
                m_context.Error(expression, ex);
                value = new StringToken(expression.FileId, expression.Line, expression.Column, String.Empty);
            }

            // Move to the new value
            m_current = expressionState.Next(value, removeBytes);
        }

        private void StartMappingInsertion()
        {
            // The template looks like:
            //
            //   jobs:
            //   - job: a
            //     variables:
            //       ${{ insert }}: ${{ parameters.jobVariables }}
            //
            // The current state looks like:
            //
            //   MappingState       // The document starts with a mapping
            //
            //   SequenceState      // The "jobs" sequence
            //
            //   MappingState       // The "job" mapping
            //
            //   MappingState       // The "variables" mapping
            //
            //   InsertExpressionState  // m_current

            var expressionState = m_current as InsertExpressionState;
            var parentMappingState = expressionState.Parent as MappingState;
            var nestedValue = parentMappingState.Value[parentMappingState.Index].Value;
            var nestedMapping = nestedValue as MappingToken;
            var removeBytes = 0;
            if (nestedMapping != null)
            {
                // Intentionally empty
            }
            else if (nestedValue is BasicExpressionToken basicExpression)
            {
                // The expression should evaluate to a mapping
                try
                {
                    nestedMapping = basicExpression.EvaluateMappingToken(expressionState.Context, out removeBytes);
                }
                catch (Exception ex)
                {
                    m_context.Error(basicExpression, ex);
                    nestedMapping = null;
                }
            }
            else
            {
                m_context.Error(nestedValue, TemplateStrings.ExpectedMapping());
                nestedMapping = null;
            }

            // Move to the nested first key
            if (nestedMapping?.Count > 0)
            {
                m_current = expressionState.Next(nestedMapping, removeBytes);
            }
            // Move to the expression end
            else
            {
                if (removeBytes > 0)
                {
                    m_memory.SubtractBytes(removeBytes);
                }

                expressionState.End();
            }
        }

        private void StartIfInsertion()
        {
            // The template looks like:
            //
            //   jobs:
            //   - job: a
            //     variables:
            //       ${{ if (github.event_name == 'push') }}: ${{ parameters.jobVariables }}
            //
            // The current state looks like:
            //
            //   MappingState       // The document starts with a mapping
            //
            //   SequenceState      // The "jobs" sequence
            //
            //   MappingState       // The "job" mapping
            //
            //   MappingState       // The "variables" mapping
            //
            //   ConditionalExpressionState  // m_current

            var expressionState = m_current as ConditionalExpressionState;
            var parentMappingState = expressionState.Parent as MappingState;
            var parentSequenceState = expressionState.Parent as SequenceState;
            bool skip = false;
            bool metastate = false;
            if(expressionState.Value.Type == TokenType.IfExpression || (parentMappingState != null && parentMappingState.IfExpressionResults.TryGetValue(parentMappingState.Index - 1, out skip) || parentSequenceState != null && parentSequenceState.IfExpressionResults.TryGetValue(parentSequenceState.Index - 1, out skip)) && skip) {
                metastate = skip = expressionState.Value.Type == TokenType.ElseExpression ? false : !new BasicExpressionToken(null, null, null, expressionState.Value.Condition).EvaluateTemplateToken(m_context, out _).AssertBoolean("bool").Value;
            } else {
                skip = true;
                metastate = false;
            }
            if(parentSequenceState == null) {
                parentMappingState.IfExpressionResults[parentMappingState.Index] = metastate;
                var nestedValue = parentMappingState.Value[parentMappingState.Index].Value;
                var nestedMapping = nestedValue as MappingToken;
                var removeBytes = 0;
                if (skip)
                {
                    nestedMapping = null;
                }
                else if (nestedMapping != null)
                {
                    // Intentionally empty
                }
                else if (nestedValue is BasicExpressionToken basicExpression)
                {
                    // The expression should evaluate to a mapping
                    try
                    {
                        nestedMapping = basicExpression.EvaluateMappingToken(expressionState.Context, out removeBytes);
                    }
                    catch (Exception ex)
                    {
                        m_context.Error(basicExpression, ex);
                        nestedMapping = null;
                    }
                }
                else
                {
                    m_context.Error(nestedValue, TemplateStrings.ExpectedMapping());
                    nestedMapping = null;
                }

                // Move to the nested first key
                if (nestedMapping?.Count > 0)
                {
                    m_current = expressionState.Next(nestedMapping, removeBytes);
                }
                // Move to the expression end
                else
                {
                    if (removeBytes > 0)
                    {
                        m_memory.SubtractBytes(removeBytes);
                    }

                    expressionState.End();
                }
            } else {
                parentSequenceState.IfExpressionResults[parentSequenceState.Index] = metastate;
                var nestedValue = (parentSequenceState.Value[parentSequenceState.Index] as MappingToken)[0].Value;
                var nestedSequence = nestedValue as SequenceToken;
                var removeBytes = 0;
                if (skip)
                {
                    nestedSequence = null;
                }
                else if (nestedSequence != null)
                {
                    // Intentionally empty
                }
                else if (nestedValue is BasicExpressionToken basicExpression)
                {
                    // The expression should evaluate to a mapping
                    try
                    {
                        nestedSequence = basicExpression.EvaluateSequenceToken(expressionState.Context, out removeBytes);
                    }
                    catch (Exception ex)
                    {
                        m_context.Error(basicExpression, ex);
                        nestedSequence = null;
                    }
                }
                else
                {
                    m_context.Error(nestedValue, TemplateStrings.ExpectedMapping());
                    nestedSequence = null;
                }

                // Move to the nested first key
                if (nestedSequence?.Count > 0)
                {
                    m_current = expressionState.Next(nestedSequence, removeBytes);
                }
                // Move to the expression end
                else
                {
                    if (removeBytes > 0)
                    {
                        m_memory.SubtractBytes(removeBytes);
                    }

                    expressionState.End();
                }
            }
        }

        private void EndExpression()
        {
            // End of document
            if (m_current.Parent == null)
            {
                m_current.Remove();
                m_current = null;
            }
            // End basic expression
            else if (m_current is BasicExpressionState)
            {
                // Move to the next item or sequence end
                if (m_current.Parent is SequenceState parentSequenceState)
                {
                    m_current.Remove();
                    m_current = parentSequenceState.Next();
                }
                // Move to the next key, next value, or mapping end
                else
                {
                    m_current.Remove();
                    var parentMappingState = m_current.Parent as MappingState;
                    m_current = parentMappingState.Next();
                }
            }
            // End "insert" mapping insertion
            else
            {
                // Move to the next key or mapping end
                m_current.Remove();
                var parentMappingState = m_current.Parent as MappingState;
                if(parentMappingState != null) {
                    parentMappingState.Next().Remove(); // Skip the value
                    m_current = parentMappingState.Next();
                } else {
                    var parentSequenceState = m_current.Parent as SequenceState;
                    m_current = parentSequenceState.Next();
                }
            }
        }

        private void UnexpectedState()
        {
            throw new InvalidOperationException("Expected state while unraveling expressions. " + DumpState());
        }

        private abstract class ReaderState
        {
            public ReaderState(
                ReaderState parent,
                TemplateToken value,
                TemplateContext context)
            {
                Parent = parent;
                Value = value;
                Context = context;
            }

            public static ReaderState CreateState(
                ReaderState parent,
                TemplateToken value,
                TemplateContext context,
                Int32 removeBytes = 0)
            {
                switch (value.Type)
                {
                    case TokenType.Null:
                    case TokenType.Boolean:
                    case TokenType.Number:
                    case TokenType.String:
                        return new LiteralState(parent, value as LiteralToken, context, removeBytes);

                    case TokenType.Sequence:
                        return new SequenceState(parent, value as SequenceToken, context, removeBytes);

                    case TokenType.Mapping:
                        return new MappingState(parent, value as MappingToken, context, removeBytes);

                    case TokenType.BasicExpression:
                        return new BasicExpressionState(parent, value as BasicExpressionToken, context, removeBytes);

                    case TokenType.InsertExpression:
                        if (removeBytes > 0)
                        {
                            throw new InvalidOperationException($"Unexpected {nameof(removeBytes)}");
                        }

                        return new InsertExpressionState(parent, value as InsertExpressionToken, context);
                    case TokenType.IfExpression:
                    case TokenType.ElseIfExpression:
                    case TokenType.ElseExpression:
                        if (removeBytes > 0)
                        {
                            throw new InvalidOperationException($"Unexpected {nameof(removeBytes)}");
                        }

                        return new ConditionalExpressionState(parent, value as ConditionalExpressionToken, context);
                    case TokenType.EachExpression:
                        if (removeBytes > 0)
                        {
                            throw new InvalidOperationException($"Unexpected {nameof(removeBytes)}");
                        }

                        return new EachExpressionState(parent, value as EachExpressionToken, context);
                    
                    default:
                        throw new NotSupportedException($"Unexpected {nameof(ReaderState)} type: {value?.GetType().Name}");
                }
            }

            public ReaderState Parent { get; }
            public TemplateContext Context { get; protected set; }
            public TemplateToken Value { get; }

            public abstract void Remove();
        }

        private abstract class ReaderState<T> : ReaderState
            where T : class
        {
            public ReaderState(
                ReaderState parent,
                TemplateToken value,
                TemplateContext context)
                : base(parent, value, context)
            {
            }

            public new T Value
            {
                get
                {
                    if (!Object.ReferenceEquals(base.Value, m_value))
                    {
                        m_value = base.Value as T;
                    }

                    return m_value;
                }
            }

            private T m_value;
        }

        private sealed class LiteralState : ReaderState<LiteralToken>
        {
            public LiteralState(
                ReaderState parent,
                LiteralToken literal,
                TemplateContext context,
                Int32 removeBytes)
                : base(parent, literal, context)
            {
                context.Memory.AddBytes(literal);
                context.Memory.IncrementDepth();
                m_removeBytes = removeBytes;
            }

            public override void Remove()
            {
                Context.Memory.SubtractBytes(Value);
                Context.Memory.DecrementDepth();

                // Subtract the memory overhead of the template token.
                // We are now done traversing it and pointers to it no longer need to exist.
                if (m_removeBytes > 0)
                {
                    Context.Memory.SubtractBytes(m_removeBytes);
                }
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine($"{GetType().Name}");
                return result.ToString();
            }

            private Int32 m_removeBytes;
        }

        private sealed class SequenceState : ReaderState<SequenceToken>
        {
            public SequenceState(
                ReaderState parent,
                SequenceToken sequence,
                TemplateContext context,
                Int32 removeBytes)
                : base(parent, sequence, context)
            {
                context.Memory.AddBytes(sequence);
                context.Memory.IncrementDepth();
                m_removeBytes = removeBytes;
            }

            public Dictionary<int, bool> IfExpressionResults { get; } = new Dictionary<int, bool>();

            /// <summary>
            /// Indicates whether the state represents the sequence-start event
            /// </summary>
            public Boolean IsStart { get; private set; } = true;

            /// <summary>
            /// The current index within the sequence
            /// </summary>
            public Int32 Index { get; private set; }

            /// <summary>
            /// Indicates whether the state represents the sequence-end event
            /// </summary>
            public Boolean IsEnd => !IsStart && Index >= Value.Count;

            public ReaderState Next()
            {
                // Adjust the state
                if (IsStart)
                {
                    IsStart = false;
                }
                else
                {
                    Index++;
                }

                // Return the next event
                if (!IsEnd)
                {
                    return CreateState(this, Value[Index], Context);
                }
                else
                {
                    return this;
                }
            }

            public ReaderState End()
            {
                IsStart = false;
                Index = Value.Count;
                return this;
            }

            public override void Remove()
            {
                Context.Memory.SubtractBytes(Value);
                Context.Memory.DecrementDepth();

                // Subtract the memory overhead of the template token.
                // We are now done traversing it and pointers to it no longer need to exist.
                if (m_removeBytes > 0)
                {
                    Context.Memory.SubtractBytes(m_removeBytes);
                }
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine($"{GetType().Name}:");
                result.AppendLine($"  IsStart: {IsStart}");
                result.AppendLine($"  Index: {Index}");
                result.AppendLine($"  IsEnd: {IsEnd}");
                return result.ToString();
            }

            private Int32 m_removeBytes;
        }

        private sealed class MappingState : ReaderState<MappingToken>
        {
            public MappingState(
                ReaderState parent,
                MappingToken mapping,
                TemplateContext context,
                Int32 removeBytes)
                : base(parent, mapping, context)
            {
                context.Memory.AddBytes(mapping);
                context.Memory.IncrementDepth();
                m_removeBytes = removeBytes;
            }

            public Dictionary<int, bool> IfExpressionResults { get; } = new Dictionary<int, bool>();

            /// <summary>
            /// Indicates whether the state represents the mapping-start event
            /// </summary>
            public Boolean IsStart { get; private set; } = true;

            /// <summary>
            /// The current index within the mapping
            /// </summary>
            public Int32 Index { get; private set; }

            /// <summary>
            /// Indicates whether the state represents a mapping-key position
            /// </summary>
            public Boolean IsKey { get; private set; }

            /// <summary>
            /// Indicates whether the state represents the mapping-end event
            /// </summary>
            public Boolean IsEnd => !IsStart && Index >= Value.Count;

            public ReaderState Next()
            {
                // Adjust the state
                if (IsStart)
                {
                    IsStart = false;
                    IsKey = true;
                }
                else if (IsKey)
                {
                    IsKey = false;
                }
                else
                {
                    Index++;
                    IsKey = true;
                }

                // Return the next event
                if (!IsEnd)
                {
                    if (IsKey)
                    {
                        return CreateState(this, Value[Index].Key, Context);
                    }
                    else
                    {
                        return CreateState(this, Value[Index].Value, Context);
                    }
                }
                else
                {
                    return this;
                }
            }

            public ReaderState End()
            {
                IsStart = false;
                Index = Value.Count;
                return this;
            }

            public override void Remove()
            {
                Context.Memory.SubtractBytes(Value);
                Context.Memory.DecrementDepth();

                // Subtract the memory overhead of the template token.
                // We are now done traversing it and pointers to it no longer need to exist.
                if (m_removeBytes > 0)
                {
                    Context.Memory.SubtractBytes(m_removeBytes);
                }
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine($"{GetType().Name}:");
                result.AppendLine($"  IsStart: {IsStart}");
                result.AppendLine($"  Index: {Index}");
                result.AppendLine($"  IsKey: {IsKey}");
                result.AppendLine($"  IsEnd: {IsEnd}");
                return result.ToString();
            }

            private Int32 m_removeBytes;
        }

        private sealed class BasicExpressionState : ReaderState<BasicExpressionToken>
        {
            public BasicExpressionState(
                ReaderState parent,
                BasicExpressionToken expression,
                TemplateContext context,
                Int32 removeBytes)
                : base(parent, expression, context)
            {
                context.Memory.AddBytes(expression);
                context.Memory.IncrementDepth();
                m_removeBytes = removeBytes;
            }

            /// <summary>
            /// Indicates whether entering the expression
            /// </summary>
            public Boolean IsStart { get; private set; } = true;

            /// <summary>
            /// Indicates whether leaving the expression
            /// </summary>
            public Boolean IsEnd => !IsStart;

            public ReaderState Next(
                TemplateToken value,
                Int32 removeBytes = 0)
            {
                // Adjust the state
                IsStart = false;

                // Return the nested state
                return CreateState(this, value, Context, removeBytes);
            }

            public ReaderState Next(
                SequenceToken value,
                Boolean isSequenceInsertion = false,
                Int32 removeBytes = 0)
            {
                // Adjust the state
                IsStart = false;

                // Create the nested state
                var nestedState = CreateState(this, value, Context, removeBytes);
                if (isSequenceInsertion)
                {
                    var nestedSequenceState = nestedState as SequenceState;
                    return nestedSequenceState.Next(); // Skip the sequence start
                }
                else
                {
                    return nestedState;
                }
            }

            public ReaderState End()
            {
                IsStart = false;
                return this;
            }

            public override void Remove()
            {
                Context.Memory.SubtractBytes(Value);
                Context.Memory.DecrementDepth();

                // Subtract the memory overhead of the template token.
                // We are now done traversing it and pointers to it no longer need to exist.
                if (m_removeBytes > 0)
                {
                    Context.Memory.SubtractBytes(m_removeBytes);
                }
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine($"{GetType().Name}:");
                result.AppendLine($"  IsStart: {IsStart}");
                return result.ToString();
            }

            private Int32 m_removeBytes;
        }

        private sealed class InsertExpressionState : ReaderState<InsertExpressionToken>
        {
            public InsertExpressionState(
                ReaderState parent,
                InsertExpressionToken expression,
                TemplateContext context)
                : base(parent, expression, context)
            {
                Context.Memory.AddBytes(expression);
                Context.Memory.IncrementDepth();
            }

            /// <summary>
            /// Indicates whether entering or leaving the expression
            /// </summary>
            public Boolean IsStart { get; private set; } = true;

            /// <summary>
            /// Indicates whether leaving the expression
            /// </summary>
            public Boolean IsEnd => !IsStart;

            public ReaderState Next(
                MappingToken value,
                Int32 removeBytes = 0)
            {
                // Adjust the state
                IsStart = false;

                // Create the nested state
                var nestedState = CreateState(this, value, Context, removeBytes) as MappingState;
                return nestedState.Next(); // Skip the mapping start
            }

            public ReaderState End()
            {
                IsStart = false;
                return this;
            }

            /// <summary>
            /// This happens when the expression is not allowed
            /// </summary>
            public ReaderState ToStringToken()
            {
                var literal = new StringToken(Value.FileId, Value.Line, Value.Column, $"{TemplateConstants.OpenExpression} {Value.Directive} {TemplateConstants.CloseExpression}");
                return CreateState(Parent, literal, Context);
            }

            public override void Remove()
            {
                Context.Memory.SubtractBytes(Value);
                Context.Memory.DecrementDepth();
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine($"{GetType().Name}:");
                result.AppendLine($"  IsStart: {IsStart}");
                return result.ToString();
            }
        }

        private sealed class ConditionalExpressionState : ReaderState<ConditionalExpressionToken>
        {
            public ConditionalExpressionState(
                ReaderState parent,
                ConditionalExpressionToken expression,
                TemplateContext context)
                : base(parent, expression, context)
            {
                Context.Memory.AddBytes(expression);
                Context.Memory.IncrementDepth();
            }

            /// <summary>
            /// Indicates whether entering or leaving the expression
            /// </summary>
            public Boolean IsStart { get; private set; } = true;

            /// <summary>
            /// Indicates whether leaving the expression
            /// </summary>
            public Boolean IsEnd => !IsStart;

            public ReaderState Next(
                MappingToken value,
                Int32 removeBytes = 0)
            {
                // Adjust the state
                IsStart = false;

                // Create the nested state
                var nestedState = CreateState(this, value, Context, removeBytes) as MappingState;
                return nestedState.Next(); // Skip the mapping start
            }

            public ReaderState Next(
                SequenceToken value,
                Int32 removeBytes = 0)
            {
                // Adjust the state
                IsStart = false;

                // Create the nested state
                var nestedState = CreateState(this, value, Context, removeBytes) as SequenceState;
                return nestedState.Next(); // Skip the sequence start
            }

            public ReaderState End()
            {
                IsStart = false;
                return this;
            }

            /// <summary>
            /// This happens when the expression is not allowed
            /// </summary>
            public ReaderState ToStringToken()
            {
                var literal = new StringToken(Value.FileId, Value.Line, Value.Column, Value.ToString());
                return CreateState(Parent, literal, Context);
            }

            public override void Remove()
            {
                Context.Memory.SubtractBytes(Value);
                Context.Memory.DecrementDepth();
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine($"{GetType().Name}:");
                result.AppendLine($"  IsStart: {IsStart}");
                return result.ToString();
            }
        }

        private sealed class EachExpressionState : ReaderState<EachExpressionToken>
        {
            public EachExpressionState(
                ReaderState parent,
                EachExpressionToken expression,
                TemplateContext context)
                : base(parent, expression, context)
            {
                Context.Memory.AddBytes(expression);
                Context.Memory.IncrementDepth();
                i = 0;
            }

            public TemplateToken Content { get; set; }
            public SequenceToken Sequence { get; set; }

            /// <summary>
            /// Indicates whether entering or leaving the expression
            /// </summary>
            public Boolean IsStart => Sequence == null || i == 0 && !IsEnd;

            /// <summary>
            /// Indicates whether leaving the expression
            /// </summary>
            public Boolean IsEnd => Sequence != null && i >= (Sequence?.Count ?? 0);

            private int i;

            public ReaderState Next(
                MappingToken value,
                Int32 removeBytes = 0)
            {
                // Adjust the state
                i++;//IsStart = false;

                // Create the nested state
                var nestedState = CreateState(this, value, Context, removeBytes) as MappingState;
                return nestedState.Next(); // Skip the mapping start
            }

            public ReaderState Next(
                SequenceToken value,
                Int32 removeBytes = 0)
            {
                // Adjust the state
                i++;//IsStart = false;

                // Create the nested state
                var nestedState = CreateState(this, value, Context, removeBytes) as SequenceState;
                return nestedState.Next(); // Skip the sequence start
            }

            public ReaderState End()
            {
                i = Sequence?.Count ?? 0;
                return this;
            }

            /// <summary>
            /// This happens when the expression is not allowed
            /// </summary>
            public ReaderState ToStringToken()
            {
                var literal = new StringToken(Value.FileId, Value.Line, Value.Column, Value.ToString());
                return CreateState(Parent, literal, Context);
            }

            public override void Remove()
            {
                Context.Memory.SubtractBytes(Value);
                Context.Memory.DecrementDepth();
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine($"{GetType().Name}:");
                result.AppendLine($"  IsStart: {IsStart}");
                return result.ToString();
            }

            public ReaderState MoveNext(TemplateContext context)
            {
                context.ExpressionValues[Value.Variable] = Sequence[i];
                return Next(Content.Clone() as SequenceToken);
            }

            public ReaderState InsertMoveNext(TemplateContext context)
            {
                context.ExpressionValues[Value.Variable] = Sequence[i];
                var expressionState = this;
                var parentMappingState = expressionState.Parent as MappingState;
                var nestedValue = parentMappingState.Value[parentMappingState.Index].Value.Clone();
                var nestedMapping = nestedValue as MappingToken;
                var removeBytes = 0;
                if (nestedMapping != null)
                {
                    // Intentionally empty
                }
                else if (nestedValue is BasicExpressionToken basicExpression)
                {
                    // The expression should evaluate to a mapping
                    try
                    {
                        nestedMapping = basicExpression.EvaluateMappingToken(expressionState.Context, out removeBytes);
                    }
                    catch (Exception ex)
                    {
                        Context.Error(basicExpression, ex);
                        nestedMapping = null;
                    }
                }
                else
                {
                    Context.Error(nestedValue, TemplateStrings.ExpectedMapping());
                    nestedMapping = null;
                }

                // Move to the nested first key
                if (nestedMapping?.Count > 0)
                {
                    return expressionState.Next(nestedMapping, removeBytes);
                }
                // Move to the expression end
                else
                {
                    if (removeBytes > 0)
                    {
                        this.Context.Memory.SubtractBytes(removeBytes);
                    }

                    expressionState.End();
                    return this;
                }
            }
        }

        private readonly TemplateContext m_context;
        private readonly TemplateMemory m_memory;
        private ReaderState m_current;
        private Boolean m_expanded;
    }
}
