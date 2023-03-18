#nullable enable

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using GitHub.DistributedTask.WebApi;
using Xunit;
using System.Text;

namespace GitHub.Runner.Common.Tests.DistributedTask
{
    public sealed class TimelineRecordL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_Defaults()
        {
            var tr = new TimelineRecord();

            Assert.Equal(0, tr.ErrorCount);
            Assert.Equal(0, tr.WarningCount);
            Assert.Equal(0, tr.NoticeCount);
            Assert.Equal(1, tr.Attempt);
            Assert.NotNull(tr.Issues);
            Assert.NotNull(tr.PreviousAttempts);
            Assert.NotNull(tr.Variables);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_Clone()
        {
            var original = new TimelineRecord();
            original.ErrorCount = 100;
            original.WarningCount = 200;
            original.NoticeCount = 300;
            original.Attempt = 3;

            // The Variables dictionary should be a case-insensitive dictionary.
            original.Variables["xxx"] = new VariableValue("first", false);
            original.Variables["XXX"] = new VariableValue("second", false);

            Assert.Equal(1, original.Variables.Count);
            Assert.Equal("second", original.Variables.Values.First().Value);
            Assert.Equal("second", original.Variables["xXx"].Value);

            var clone = original.Clone();

            Assert.NotSame(original, clone);
            Assert.NotSame(original.Variables, clone.Variables);
            Assert.Equal(100, clone.ErrorCount);
            Assert.Equal(200, clone.WarningCount);
            Assert.Equal(300, clone.NoticeCount);
            Assert.Equal(3, clone.Attempt);

            // Now, mutate the original post-clone.
            original.ErrorCount++;
            original.WarningCount += 10;
            original.NoticeCount *= 3;
            original.Attempt--;
            original.Variables["a"] = new VariableValue("1", false);

            // Verify that the clone was unaffected by the changes to the original.
            Assert.Equal(100, clone.ErrorCount);
            Assert.Equal(200, clone.WarningCount);
            Assert.Equal(300, clone.NoticeCount);
            Assert.Equal(3, clone.Attempt);
            Assert.Equal(1, clone.Variables.Count);
            Assert.Equal("second", clone.Variables.Values.First().Value);

            // Verify that the clone's Variables dictionary is also case-sensitive.
            clone.Variables["yyy"] = new VariableValue("third", false);
            clone.Variables["YYY"] = new VariableValue("fourth", false);

            Assert.Equal(2, clone.Variables.Count);
            Assert.Equal("second", clone.Variables["xXx"].Value);
            Assert.Equal("fourth", clone.Variables["yYy"].Value);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_DeserializationEdgeCase_NonNullCollections()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            // Verify that missing JSON fields don't result in null values for collection properties.
            var tr = Deserialize(jsonSamples["minimal"]);
            Assert.NotNull(tr);
            Assert.Equal("minimal", tr!.Name);
            Assert.NotNull(tr.Issues);
            Assert.NotNull(tr.PreviousAttempts);
            Assert.NotNull(tr.Variables);

            // Verify that explicitly-null JSON fields don't result in null values for collection properties.
            // (Our deserialization logic should fix these up and instantiate an empty collection.)
            tr = Deserialize(jsonSamples["explicit-null-collections"]);
            Assert.NotNull(tr);
            Assert.Equal("explicit-null-collections", tr!.Name);
            Assert.NotNull(tr.Issues);
            Assert.NotNull(tr.PreviousAttempts);
            Assert.NotNull(tr.Variables);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_DeserializationEdgeCase_AttemptCannotBeLessThan1()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            // Verify that 1 is the effective floor for TimelineRecord::Attempt.
            var tr = Deserialize(jsonSamples["minimal"]);
            Assert.NotNull(tr);
            Assert.Equal("minimal", tr!.Name);
            Assert.Equal(1, tr.Attempt);

            tr = Deserialize(jsonSamples["invalid-attempt-value"]);
            Assert.NotNull(tr);
            Assert.Equal("invalid-attempt-value", tr!.Name);
            Assert.Equal(1, tr.Attempt);

            tr = Deserialize(jsonSamples["zero-attempt-value"]);
            Assert.NotNull(tr);
            Assert.Equal("zero-attempt-value", tr!.Name);
            Assert.Equal(1, tr.Attempt);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_DeserializationEdgeCase_HandleLegacyNullsGracefully()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            // Verify that nulls for ErrorCount, WarningCount, and NoticeCount are interpreted as 0.
            var tr = Deserialize(jsonSamples["legacy-nulls"]);
            Assert.NotNull(tr);
            Assert.Equal("legacy-nulls", tr!.Name);
            Assert.Equal(0, tr.ErrorCount);
            Assert.Equal(0, tr.WarningCount);
            Assert.Equal(0, tr.NoticeCount);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_DeserializationEdgeCase_HandleMissingCountsGracefully()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            // Verify that nulls for ErrorCount, WarningCount, and NoticeCount are interpreted as 0.
            var tr = Deserialize(jsonSamples["missing-counts"]);
            Assert.NotNull(tr);
            Assert.Equal("missing-counts", tr!.Name);
            Assert.Equal(0, tr.ErrorCount);
            Assert.Equal(0, tr.WarningCount);
            Assert.Equal(0, tr.NoticeCount);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_DeserializationEdgeCase_NonZeroCounts()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            // Verify that nulls for ErrorCount, WarningCount, and NoticeCount are interpreted as 0.
            var tr = Deserialize(jsonSamples["non-zero-counts"]);
            Assert.NotNull(tr);
            Assert.Equal("non-zero-counts", tr!.Name);
            Assert.Equal(10, tr.ErrorCount);
            Assert.Equal(20, tr.WarningCount);
            Assert.Equal(30, tr.NoticeCount);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_Deserialization_LeanTimelineRecord()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            // Verify that a lean TimelineRecord can be deserialized.
            var tr = Deserialize(jsonSamples["lean"]);
            Assert.NotNull(tr);
            Assert.Equal("lean", tr!.Name);
            Assert.Equal(4, tr.Attempt);
            Assert.Equal(1, tr.Issues.Count);
            Assert.Equal(3, tr.Variables.Count);
            Assert.Equal(3, tr.PreviousAttempts.Count);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_Deserialization_VariablesDictionaryIsCaseInsensitive()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            var tr = Deserialize(jsonSamples["lean"]);
            Assert.NotNull(tr);
            Assert.Equal("lean", tr!.Name);
            Assert.Equal(3, tr.Variables.Count);

            // Verify that the Variables Dictionary is case-insensitive.
            tr.Variables["X"] = new VariableValue("overwritten", false);
            Assert.Equal(3, tr.Variables.Count);

            tr.Variables["new"] = new VariableValue("new.1", false);
            Assert.Equal(4, tr.Variables.Count);

            tr.Variables["NEW"] = new VariableValue("new.2", false);
            Assert.Equal(4, tr.Variables.Count);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "DistributedTask")]
        public void VerifyTimelineRecord_DeserializationEdgeCase_DuplicateVariableKeysThrowsException()
        {
            var jsonSamples = LoadJsonSamples(JsonSamplesFilePath);

            // We could be more forgiving in this case if we discover that it's not uncommon in Production for serialized TimelineRecords to:
            // 1)  get incorrectly instantiated with a case-sensitive Variables dictionary (in older versions, this was possible via TimelineRecord::Clone)
            // 2)  end up with case variations of the same key
            // 3)  make another serialization/deserialization round trip.
            //
            // If we wanted to grant clemency to such incorrectly-serialized TimelineRecords,
            // the fix to TimelineRecord::EnsureInitialized would look something like the following:
            //
            // var seedVariables = m_variables ?? Enumerable.Empty<KeyValuePair<string, VariableValue>>();
            // m_variables = new Dictionary<string, VariableValue>(seedVariables.Count(), StringComparer.OrdinalIgnoreCase);
            // foreach (var kvp in seedVariables)
            // {
            //     m_variables[kvp.Key] = kvp.Value;
            // }
            Assert.Throws<ArgumentException>(() => Deserialize(jsonSamples["duplicate-variable-keys"]));
        }


        private static Dictionary<string, string> LoadJsonSamples(string path)
        {
            // Embedding independent JSON samples within YML works well because JSON generally doesn't need to be escaped or otherwise mangled.
            var yamlDeserializer = new YamlDotNet.Serialization.Deserializer();
            using var stream = new StreamReader(path);
            return yamlDeserializer.Deserialize<Dictionary<string, string>>(stream);
        }

        private static TimelineRecord? Deserialize(string rawJson)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawJson ?? string.Empty));
            return m_jsonSerializer.ReadObject(stream) as TimelineRecord;
        }

        private static string JsonSamplesFilePath
        {
            get
            {
                return Path.Combine(TestUtil.GetTestDataPath(), "timelinerecord_json_samples.yml");
            }
        }

        private static readonly DataContractJsonSerializer m_jsonSerializer = new(typeof(TimelineRecord));
    }
}
