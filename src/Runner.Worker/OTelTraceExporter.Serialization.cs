using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GitHub.Runner.Worker
{
    public sealed partial class OTelTraceExporter
    {
        // OTLP/JSON, serialized with Utf8JsonWriter for correct escaping (a control
        // character in a step name must not be able to invalidate the whole batch).
        // Builders return the UTF-8 bytes directly — no bytes -> string -> bytes
        // round-trip — so flush never triple-materializes a large payload.
        private byte[] BuildOTLPSpansJson(List<OTelSpan> spans, List<KeyValuePair<string, object>> resource)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, s_jsonOptions))
            {
                w.WriteStartObject();
                w.WriteStartArray("resourceSpans");
                w.WriteStartObject();

                w.WriteStartObject("resource");
                w.WriteStartArray("attributes");
                WriteAttributes(w, resource);
                w.WriteEndArray();
                w.WriteEndObject();

                w.WriteStartArray("scopeSpans");
                w.WriteStartObject();
                w.WriteStartObject("scope");
                w.WriteString("name", "github.actions.runner");
                if (!string.IsNullOrEmpty(_serviceVersion)) w.WriteString("version", _serviceVersion);
                w.WriteEndObject();

                w.WriteStartArray("spans");
                foreach (var s in spans)
                {
                    w.WriteStartObject();
                    w.WriteString("traceId", s.TraceId);
                    w.WriteString("spanId", s.SpanId);
                    if (!string.IsNullOrEmpty(s.ParentSpanId))
                    {
                        w.WriteString("parentSpanId", s.ParentSpanId);
                    }
                    w.WriteString("name", Mask(s.Name) ?? "");
                    w.WriteNumber("kind", s.Kind);
                    w.WriteString("startTimeUnixNano", s.StartTimeUnixNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteString("endTimeUnixNano", s.EndTimeUnixNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteStartArray("attributes");
                    WriteAttributes(w, s.Attributes);
                    w.WriteEndArray();
                    if (s.Links.Count > 0)
                    {
                        w.WriteStartArray("links");
                        foreach (var lk in s.Links)
                        {
                            w.WriteStartObject();
                            w.WriteString("traceId", lk.TraceId);
                            w.WriteString("spanId", lk.SpanId);
                            if (!string.IsNullOrEmpty(lk.TraceState))
                            {
                                w.WriteString("traceState", lk.TraceState);
                            }
                            if (lk.Flags != 0)
                            {
                                w.WriteNumber("flags", lk.Flags);
                            }
                            w.WriteStartArray("attributes");
                            WriteAttributes(w, lk.Attributes);
                            w.WriteEndArray();
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                    }
                    if (s.Events.Count > 0)
                    {
                        w.WriteStartArray("events");
                        foreach (var ev in s.Events)
                        {
                            w.WriteStartObject();
                            w.WriteString("name", ev.Name);
                            w.WriteString("timeUnixNano", ev.TimeUnixNano.ToString(CultureInfo.InvariantCulture));
                            w.WriteStartArray("attributes");
                            WriteAttributes(w, ev.Attributes);
                            w.WriteEndArray();
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                    }
                    w.WriteStartObject("status");
                    if (s.StatusCode != 0)
                    {
                        w.WriteNumber("code", s.StatusCode);
                    }
                    w.WriteEndObject();
                    w.WriteEndObject();
                }
                w.WriteEndArray(); // spans
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // scopeSpans[0]
                w.WriteEndArray();  // scopeSpans
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // resourceSpans[0]
                w.WriteEndArray();  // resourceSpans
                w.WriteEndObject();
            }
            return stream.ToArray();
        }

        private void WriteAttributes(Utf8JsonWriter w, List<KeyValuePair<string, object>> attrs)
        {
            foreach (var kv in attrs)
            {
                w.WriteStartObject();
                w.WriteString("key", kv.Key);
                WriteAnyValue(w, "value", kv.Value);
                w.WriteEndObject();
            }
        }

        // OTLP AnyValue: each CLR type must land on its own wire type — silently
        // stringifying a double (or an array) gives downstream numeric queries the
        // wrong type with no error anywhere. Strings are masked; genuinely
        // unsupported types fall back to their masked string form (documented,
        // not silent: this is the only stringify arm).
        private void WriteAnyValue(Utf8JsonWriter w, string propertyName, object value)
        {
            w.WriteStartObject(propertyName);
            switch (value)
            {
                case string s: // before IEnumerable — string is IEnumerable<char>
                    w.WriteString("stringValue", Mask(s) ?? "");
                    break;
                case byte[] bytes:
                    w.WriteString("bytesValue", Convert.ToBase64String(bytes));
                    break;
                case System.Collections.IEnumerable items:
                    w.WriteStartObject("arrayValue");
                    w.WriteStartArray("values");
                    foreach (var item in items)
                    {
                        w.WriteStartObject();
                        WriteScalarAnyValueBody(w, item);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                    w.WriteEndObject();
                    break;
                default:
                    WriteScalarAnyValueBody(w, value);
                    break;
            }
            w.WriteEndObject();
        }

        // Scalar AnyValue fields, written into the current object (also used for
        // arrayValue elements, which have no wrapper key). Nested arrays are
        // stringified — one level is all the attribute API promises.
        private void WriteScalarAnyValueBody(Utf8JsonWriter w, object value)
        {
            switch (value)
            {
                case bool b:
                    w.WriteBoolean("boolValue", b);
                    break;
                case long l:
                    w.WriteString("intValue", l.ToString(CultureInfo.InvariantCulture));
                    break;
                case int n:
                    w.WriteString("intValue", n.ToString(CultureInfo.InvariantCulture));
                    break;
                case double d:
                    w.WriteNumber("doubleValue", d);
                    break;
                case float f:
                    w.WriteNumber("doubleValue", f);
                    break;
                default:
                    w.WriteString("stringValue", Mask(value?.ToString()) ?? "");
                    break;
            }
        }

        private byte[] BuildOTLPLogsJson(List<OTelLog> logs, List<KeyValuePair<string, object>> resource)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, s_jsonOptions))
            {
                w.WriteStartObject();
                w.WriteStartArray("resourceLogs");
                w.WriteStartObject();
                w.WriteStartObject("resource");
                w.WriteStartArray("attributes");
                WriteAttributes(w, resource);
                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteStartArray("scopeLogs");
                w.WriteStartObject();
                w.WriteStartObject("scope");
                w.WriteString("name", "github.actions.runner");
                if (!string.IsNullOrEmpty(_serviceVersion)) w.WriteString("version", _serviceVersion);
                w.WriteEndObject();
                w.WriteStartArray("logRecords");
                foreach (var l in logs)
                {
                    w.WriteStartObject();
                    w.WriteString("timeUnixNano", l.TimeUnixNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteString("observedTimeUnixNano", l.TimeUnixNano.ToString(CultureInfo.InvariantCulture));
                    w.WriteNumber("severityNumber", l.SeverityNumber);
                    w.WriteString("severityText", l.SeverityText);
                    w.WriteStartObject("body");
                    w.WriteString("stringValue", Mask(l.Body) ?? "");
                    w.WriteEndObject();
                    w.WriteString("traceId", l.TraceId);
                    w.WriteString("spanId", l.SpanId);
                    w.WriteStartArray("attributes");
                    WriteAttributes(w, l.Attributes);
                    w.WriteEndArray();
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject();
            }
            return stream.ToArray();
        }

        private sealed class OTelLog
        {
            public long TimeUnixNano;
            public int SeverityNumber;
            public string SeverityText;
            public string Body;
            public string TraceId;
            public string SpanId;
            public readonly List<KeyValuePair<string, object>> Attributes = new();
        }

        private sealed class JobMetrics
        {
            public long StartNano;
            public long EndNano;
            public double DurationSeconds;
            public string PipelineName;
            public string Attempt;
            public string Result;
            public int Errors;
        }

        // Per-task (job or step) duration observation -> cicd.pipeline.task.duration.
        private sealed class TaskMetric
        {
            public string Name;
            public string Type;   // "job" | "step"
            public string Result; // semconv result (success|failure|skip|...)
            public string Attempt;
            public double DurationSeconds;
            public long StartNano;
            public long EndNano;
        }

        // Explicit duration buckets (seconds) so the collector can aggregate real
        // percentiles across runs, instead of an empty-bounds (single implicit bucket)
        // histogram. Mirrors what the collector's spanmetrics connector would emit.
        private static readonly double[] s_durationBucketsSeconds = { 1, 2.5, 5, 10, 30, 60, 120, 300, 600, 1800 };

        // Write a histogram dataPoint body for ONE duration observation: places it in the
        // right explicit bucket so cumulative aggregation in the collector is meaningful.
        private static void WriteSingleObservationHistogram(Utf8JsonWriter w, double seconds, long startNano, long endNano)
        {
            var bounds = s_durationBucketsSeconds;
            var bucket = bounds.Length; // overflow bucket unless an upper bound matches
            for (var i = 0; i < bounds.Length; i++)
            {
                if (seconds <= bounds[i]) { bucket = i; break; }
            }
            w.WriteString("startTimeUnixNano", startNano.ToString(CultureInfo.InvariantCulture));
            w.WriteString("timeUnixNano", endNano.ToString(CultureInfo.InvariantCulture));
            w.WriteString("count", "1");
            w.WriteNumber("sum", seconds);
            w.WriteStartArray("bucketCounts");
            for (var i = 0; i <= bounds.Length; i++) { w.WriteStringValue(i == bucket ? "1" : "0"); }
            w.WriteEndArray();
            w.WriteStartArray("explicitBounds");
            foreach (var b in bounds) { w.WriteNumberValue(b); }
            w.WriteEndArray();
            w.WriteNumber("min", seconds);
            w.WriteNumber("max", seconds);
        }

        // OTLP/JSON metrics: github.pipeline.run.duration (histogram) + github.pipeline.run.errors
        // (counter) + github.pipeline.task.duration (histogram, one point per job/step). Vendor
        // (github.*) namespace deliberately — these are not registered cicd.* semconv metrics.
        // CUMULATIVE temporality is valid: each per-run process exports one point with its own
        // startTimeUnixNano; the collector aggregates across runs.
        private byte[] BuildOTLPMetricsJson(JobMetrics m, List<TaskMetric> tasks, List<KeyValuePair<string, object>> resource)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, s_jsonOptions))
            {
                w.WriteStartObject();
                w.WriteStartArray("resourceMetrics");
                w.WriteStartObject();
                w.WriteStartObject("resource");
                w.WriteStartArray("attributes");
                WriteAttributes(w, resource);
                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteStartArray("scopeMetrics");
                w.WriteStartObject();
                w.WriteStartObject("scope");
                w.WriteString("name", "github.actions.runner");
                if (!string.IsNullOrEmpty(_serviceVersion)) w.WriteString("version", _serviceVersion);
                w.WriteEndObject();
                w.WriteStartArray("metrics");

                if (m != null)
                {
                    // github.pipeline.run.duration — histogram with a single observation.
                    w.WriteStartObject();
                    w.WriteString("name", "github.pipeline.run.duration");
                    w.WriteString("unit", "s");
                    w.WriteStartObject("histogram");
                    w.WriteNumber("aggregationTemporality", 2); // CUMULATIVE
                    w.WriteStartArray("dataPoints");
                    w.WriteStartObject();
                    w.WriteStartArray("attributes");
                    WriteAttributes(w, new List<KeyValuePair<string, object>>
                    {
                        new("cicd.pipeline.name", m.PipelineName),
                        new("github.run_attempt", m.Attempt),
                        new("cicd.pipeline.result", m.Result),
                    });
                    w.WriteEndArray();
                    WriteSingleObservationHistogram(w, m.DurationSeconds, m.StartNano, m.EndNano);
                    w.WriteEndObject();
                    w.WriteEndArray();
                    w.WriteEndObject(); // histogram
                    w.WriteEndObject(); // metric

                    if (m.Errors > 0)
                    {
                        w.WriteStartObject();
                        w.WriteString("name", "github.pipeline.run.errors");
                        w.WriteString("unit", "{error}");
                        w.WriteStartObject("sum");
                        w.WriteNumber("aggregationTemporality", 2);
                        w.WriteBoolean("isMonotonic", true);
                        w.WriteStartArray("dataPoints");
                        w.WriteStartObject();
                        w.WriteStartArray("attributes");
                        WriteAttributes(w, new List<KeyValuePair<string, object>>
                        {
                            new("cicd.pipeline.name", m.PipelineName),
                            new("github.run_attempt", m.Attempt),
                            new("error.type", "failure"),
                        });
                        w.WriteEndArray();
                        w.WriteString("startTimeUnixNano", m.StartNano.ToString(CultureInfo.InvariantCulture));
                        w.WriteString("timeUnixNano", m.EndNano.ToString(CultureInfo.InvariantCulture));
                        w.WriteString("asInt", m.Errors.ToString(CultureInfo.InvariantCulture));
                        w.WriteEndObject();
                        w.WriteEndArray();
                        w.WriteEndObject();
                        w.WriteEndObject();
                    }
                }

                // github.pipeline.task.duration — histogram with one observation per job/step.
                if (tasks != null && tasks.Count > 0)
                {
                    w.WriteStartObject();
                    w.WriteString("name", "github.pipeline.task.duration");
                    w.WriteString("unit", "s");
                    w.WriteStartObject("histogram");
                    w.WriteNumber("aggregationTemporality", 2); // CUMULATIVE
                    w.WriteStartArray("dataPoints");
                    foreach (var task in tasks)
                    {
                        w.WriteStartObject();
                        w.WriteStartArray("attributes");
                        WriteAttributes(w, new List<KeyValuePair<string, object>>
                        {
                            new("cicd.pipeline.task.name", task.Name),
                            new("cicd.pipeline.task.run.result", task.Result),
                            new("github.run_attempt", task.Attempt),
                            new("github.record_type", task.Type),
                        });
                        w.WriteEndArray();
                        WriteSingleObservationHistogram(w, task.DurationSeconds, task.StartNano, task.EndNano);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                    w.WriteEndObject(); // histogram
                    w.WriteEndObject(); // metric
                }

                w.WriteEndArray(); // metrics
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // scopeMetrics[0]
                w.WriteEndArray();
                w.WriteString("schemaUrl", SchemaUrl);
                w.WriteEndObject(); // resourceMetrics[0]
                w.WriteEndArray();
                w.WriteEndObject();
            }
            return stream.ToArray();
        }

        private sealed class OTelEvent
        {
            public string Name;
            public long TimeUnixNano;
            public readonly List<KeyValuePair<string, object>> Attributes = new();
        }

        // A span link expresses cross-trace causality (W3C). Used for the inbound
        // scheduler context (e.g. ARC) so the job links to its "schedule" span
        // without re-rooting the runner's deterministic trace.
        private sealed class OTelLink
        {
            public string TraceId;
            public string SpanId;
            public string TraceState;
            public int Flags;
            public readonly List<KeyValuePair<string, object>> Attributes = new();
        }

        private sealed class OTelSpan
        {
            public string TraceId;
            public string SpanId;
            public string ParentSpanId;
            public string Name;
            public int Kind = 1;
            public long StartTimeUnixNano;
            public long EndTimeUnixNano;
            public int StatusCode; // 0 unset, 1 ok, 2 error
            public readonly List<OTelLink> Links = new();
            public readonly List<KeyValuePair<string, object>> Attributes = new();
            public readonly List<OTelEvent> Events = new();

            public void Set(string key, object value)
            {
                Attributes.Add(new KeyValuePair<string, object>(key, value));
            }

            // semconv exception event.
            public void AddException(string type, string message, long timeNano)
            {
                var ev = new OTelEvent { Name = "exception", TimeUnixNano = timeNano };
                ev.Attributes.Add(new("exception.type", type));
                if (!string.IsNullOrEmpty(message))
                {
                    ev.Attributes.Add(new("exception.message", message));
                }
                Events.Add(ev);
            }
        }
    }
}
