using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Xunit;

namespace GitHub.Actions.RunService.WebApi.Tests;

public sealed class AcquireJobRequestL0
{

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifySerialization()
    {
        var request = new AcquireJobRequest
        {
            JobMessageID = "1526919030369-33"
        };
        var serializer = new DataContractJsonSerializer(typeof(AcquireJobRequest));
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, request);

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string json = reader.ReadToEnd();
        string expected = DoubleQuotify(string.Format("{{'jobMessageId':'{0}'}}", request.JobMessageID));
        Assert.Equal(expected, json);

    }

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyDeserialization()
    {
        var serializer = new DataContractJsonSerializer(typeof(AcquireJobRequest));
        var variations = new Dictionary<string, string>()
        {
            ["{'streamId': 'legacy', 'jobMessageId': 'new-1'}"] = "new-1",
            ["{'jobMessageId': 'new-2', 'streamId': 'legacy'}"] = "new-2",
            ["{'jobMessageId': 'new-3'}"] = "new-3",
        };

        foreach (var (source, expected) in variations)
        {
            using var stream = new MemoryStream();
            stream.Write(Encoding.UTF8.GetBytes(DoubleQuotify(source)));
            stream.Position = 0;
            var recoveredRecord = serializer.ReadObject(stream) as AcquireJobRequest;
            Assert.NotNull(recoveredRecord);
            Assert.Equal(expected, recoveredRecord.JobMessageID);
        }
    }

    private static string DoubleQuotify(string text)
    {
        return text.Replace('\'', '"');
    }
}
