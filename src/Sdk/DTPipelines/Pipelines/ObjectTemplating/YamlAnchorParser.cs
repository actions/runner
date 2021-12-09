// This file is part of YamlDotNet - A .NET library for YAML.
// Copyright (c) Antoine Aubry and contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Core
{
    /// <summary>
    /// Simple implementation of <see cref="IParser"/> that implements merging: http://yaml.org/type/merge.html
    /// Now also handles anchors and removes them
    /// </summary>
    public sealed class YamlAnchorParser : IParser
    {
        private readonly ParsingEventCollection events;
        private readonly IParser innerParser;
        private IEnumerator<LinkedListNode<ParsingEvent>> iterator;
        private bool merged;

        public YamlAnchorParser(IParser innerParser)
        {
            events = new ParsingEventCollection();
            merged = false;
            iterator = events.GetEnumerator();
            this.innerParser = innerParser;
        }

        public ParsingEvent Current => iterator.Current?.Value;

        public bool MoveNext()
        {
            if (!merged)
            {
                Merge();
                iterator = events.GetEnumerator();
                merged = true;
            }

            return iterator.MoveNext();
        }

        private void Merge()
        {
            while (innerParser.MoveNext())
            {
                var cevent = innerParser.Current;
                events.Add(cevent);
            }

            // Disable Merge Operator
            // foreach (var node in events)
            // {
            //     if (IsMergeToken(node))
            //     {
            //         events.MarkDeleted(node);
            //         if (!HandleMerge(node.Next))
            //         {
            //             throw new SemanticErrorException(node.Value.Start, node.Value.End, "Unrecognized merge key pattern");
            //         }
            //     }
            // }
            // events.CleanMarked();
            foreach (var node in events)
            {
                if (node.Value is AnchorAlias anchorAlias)
                {
                    if (!HandleAnchorAlias2(node, node, anchorAlias))
                    {
                        throw new SemanticErrorException(node.Value.Start, node.Value.End, "Unrecognized anchor reference pattern");
                    }
                }
            }
            events.CleanMarked();
            foreach (var node in events)
            {
                var m_current = node.Value;
                if (m_current is Scalar scalar)
                {
                    // Verify not using achors
                    if (scalar.Anchor != null)
                    {
                        node.Value = new Scalar(null, scalar.Tag, scalar.Value, scalar.Style, scalar.IsPlainImplicit, scalar.IsQuotedImplicit, scalar.Start, scalar.End);
                    }
                }
                else if (m_current is MappingStart mappingStart)
                {
                    // Verify not using achors
                    if (mappingStart.Anchor != null)
                    {
                        node.Value = new MappingStart(null, mappingStart.Tag, mappingStart.IsImplicit, mappingStart.Style, mappingStart.Start, mappingStart.End);
                    }
                }
                else if (m_current is SequenceStart sequenceStart)
                {
                    // Verify not using achors
                    if (sequenceStart.Anchor != null)
                    {
                        node.Value = new SequenceStart(null, sequenceStart.Tag, sequenceStart.IsImplicit, sequenceStart.Style, sequenceStart.Start, sequenceStart.End);   
                    }
                }
            }
        }

        private bool HandleMerge(LinkedListNode<ParsingEvent> node)
        {
            if (node == null)
            {
                return false;
            }

            if (node.Value is AnchorAlias anchorAlias)
            {
                return HandleAnchorAlias(node, node, anchorAlias);
            }

            if (node.Value is SequenceStart)
            {
                return HandleSequence(node);
            }

            return false;
        }

        private bool HandleMergeSequence(LinkedListNode<ParsingEvent> sequenceStart, LinkedListNode<ParsingEvent> node)
        {
            if (node is null)
            {
                return false;
            }
            if (node.Value is AnchorAlias anchorAlias)
            {
                return HandleAnchorAlias(sequenceStart, node, anchorAlias);
            }
            if (node.Value is SequenceStart)
            {
                return HandleSequence(node);
            }
            return false;
        }

        private bool IsMergeToken(LinkedListNode<ParsingEvent> node)
        {
            return node.Value is Scalar merge && merge.Value == "<<";
        }

        private bool HandleAnchorAlias(LinkedListNode<ParsingEvent> node, LinkedListNode<ParsingEvent> anchorNode, AnchorAlias anchorAlias)
        {
            var mergedEvents = GetMappingEvents(anchorAlias.Value);

            events.AddAfter(node, mergedEvents);
            events.MarkDeleted(anchorNode);

            return true;
        }

        private bool HandleAnchorAlias2(LinkedListNode<ParsingEvent> node, LinkedListNode<ParsingEvent> anchorNode, AnchorAlias anchorAlias)
        {
            var mergedEvents = GetEvents(anchorAlias.Value).ToArray();

            events.AddAfter(node, mergedEvents);
            events.MarkDeleted(anchorNode);
            return true;
        }

        private bool HandleSequence(LinkedListNode<ParsingEvent> node)
        {
            var sequenceStart = node;
            events.MarkDeleted(node);

            var current = node;
            while (current != null)
            {
                if (current.Value is SequenceEnd)
                {
                    events.MarkDeleted(current);
                    return true;
                }

                var next = current.Next;
                HandleMergeSequence(sequenceStart, next);
                current = next;
            }

            return true;
        }

        private IEnumerable<ParsingEvent> GetMappingEvents(AnchorName anchor)
        {
            var cloner = new ParsingEventCloner();
            var nesting = 0;

            return events.FromAnchor(anchor)
                .Skip(1)
                .Select(e => e.Value)
                .TakeWhile(e => (nesting += e.NestingIncrease) >= 0)
                .Select(e => cloner.Clone(e));
        }

        private IEnumerable<ParsingEvent> GetEvents(AnchorName anchor)
        {
            var cloner = new ParsingEventCloner();
            var nesting = 0;
            bool prev = true;

            return events.FromAnchor(anchor)
                .Select(e => e.Value)
                .TakeWhile(e => {
                    var now = (nesting += e.NestingIncrease) >= 1;
                    if(!now && prev) {
                        prev = false;
                        return true;
                    }
                    return now;
                })
                .Select(e => cloner.Clone(e));
        }

        private sealed class ParsingEventCollection : IEnumerable<LinkedListNode<ParsingEvent>>
        {
            private readonly LinkedList<ParsingEvent> events;
            private readonly HashSet<LinkedListNode<ParsingEvent>> deleted;
            private readonly Dictionary<AnchorName, LinkedListNode<ParsingEvent>> references;

            public ParsingEventCollection()
            {
                events = new LinkedList<ParsingEvent>();
                deleted = new HashSet<LinkedListNode<ParsingEvent>>();
                references = new Dictionary<AnchorName, LinkedListNode<ParsingEvent>>();
            }

            public void AddAfter(LinkedListNode<ParsingEvent> node, IEnumerable<ParsingEvent> items)
            {
                foreach (var item in items)
                {
                    node = events.AddAfter(node, item);
                }
            }

            public void Add(ParsingEvent item)
            {
                var node = events.AddLast(item);
                AddReference(item, node);
            }

            public void MarkDeleted(LinkedListNode<ParsingEvent> node)
            {
                deleted.Add(node);
            }

            public void CleanMarked()
            {
                foreach (var node in deleted)
                {
                    events.Remove(node);
                }
                deleted.Clear();
            }

            public IEnumerable<LinkedListNode<ParsingEvent>> FromAnchor(AnchorName anchor)
            {
                var node = references[anchor];
                return Enumerate(node);
            }

            public IEnumerator<LinkedListNode<ParsingEvent>> GetEnumerator() => Enumerate(events.First).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<LinkedListNode<ParsingEvent>> Enumerate(LinkedListNode<ParsingEvent> node)
            {
                while (node != null)
                {
                    yield return node;
                    node = node.Next;
                }
            }

            private void AddReference(ParsingEvent item, LinkedListNode<ParsingEvent> node)
            {
                if (item is NodeEvent nodeEvent)
                {
                    var anchor = nodeEvent.Anchor;
                    if (!anchor.IsEmpty)
                    {
                        references[anchor] = node;
                    }
                }
            }
        }

        private sealed class ParsingEventCloner : IParsingEventVisitor
        {
            private ParsingEvent clonedEvent;

            public ParsingEvent Clone(ParsingEvent e)
            {
                e.Accept(this);
                if (clonedEvent == null)
                {
                    throw new InvalidOperationException($"Could not clone event of type");
                }

                return clonedEvent;
            }

            void IParsingEventVisitor.Visit(AnchorAlias e)
            {
                clonedEvent = new AnchorAlias(e.Value, e.Start, e.End);
            }

            void IParsingEventVisitor.Visit(StreamStart e)
            {
                throw new NotSupportedException();
            }

            void IParsingEventVisitor.Visit(StreamEnd e)
            {
                throw new NotSupportedException();
            }

            void IParsingEventVisitor.Visit(DocumentStart e)
            {
                throw new NotSupportedException();
            }

            void IParsingEventVisitor.Visit(DocumentEnd e)
            {
                throw new NotSupportedException();
            }

            void IParsingEventVisitor.Visit(Scalar e)
            {
                clonedEvent = new Scalar(AnchorName.Empty, e.Tag, e.Value, e.Style, e.IsPlainImplicit, e.IsQuotedImplicit, e.Start, e.End);
            }

            void IParsingEventVisitor.Visit(SequenceStart e)
            {
                clonedEvent = new SequenceStart(AnchorName.Empty, e.Tag, e.IsImplicit, e.Style, e.Start, e.End);
            }

            void IParsingEventVisitor.Visit(SequenceEnd e)
            {
                clonedEvent = new SequenceEnd(e.Start, e.End);
            }

            void IParsingEventVisitor.Visit(MappingStart e)
            {
                clonedEvent = new MappingStart(AnchorName.Empty, e.Tag, e.IsImplicit, e.Style, e.Start, e.End);
            }

            void IParsingEventVisitor.Visit(MappingEnd e)
            {
                clonedEvent = new MappingEnd(e.Start, e.End);
            }

            void IParsingEventVisitor.Visit(Comment e)
            {
                throw new NotSupportedException();
            }
        }
    }
}