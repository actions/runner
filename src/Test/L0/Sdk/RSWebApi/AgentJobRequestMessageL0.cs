using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Xunit;
using GitHub.DistributedTask.Pipelines;

namespace GitHub.Actions.RunService.WebApi.Tests;

public sealed class AgentJobRequestMessageL0
{
    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyEnableDebuggerDeserialization_WithTrue()
    {
        // Arrange
        var serializer = new DataContractJsonSerializer(typeof(AgentJobRequestMessage));
        string jsonWithEnabledDebugger = DoubleQuotify("{'EnableDebugger': true}");
        
        // Act
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(jsonWithEnabledDebugger));
        stream.Position = 0;
        var recoveredMessage = serializer.ReadObject(stream) as AgentJobRequestMessage;
        
        // Assert
        Assert.NotNull(recoveredMessage);
        Assert.True(recoveredMessage.EnableDebugger, "EnableDebugger should be true when JSON contains 'EnableDebugger': true");
    }

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyEnableDebuggerDeserialization_DefaultToFalse()
    {
        // Arrange
        var serializer = new DataContractJsonSerializer(typeof(AgentJobRequestMessage));
        string jsonWithoutDebugger = DoubleQuotify("{'messageType': 'PipelineAgentJobRequest'}");
        
        // Act
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(jsonWithoutDebugger));
        stream.Position = 0;
        var recoveredMessage = serializer.ReadObject(stream) as AgentJobRequestMessage;
        
        // Assert
        Assert.NotNull(recoveredMessage);
        Assert.False(recoveredMessage.EnableDebugger, "EnableDebugger should default to false when JSON field is absent");
    }

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyEnableDebuggerDeserialization_WithFalse()
    {
        // Arrange
        var serializer = new DataContractJsonSerializer(typeof(AgentJobRequestMessage));
        string jsonWithDisabledDebugger = DoubleQuotify("{'EnableDebugger': false}");
        
        // Act
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(jsonWithDisabledDebugger));
        stream.Position = 0;
        var recoveredMessage = serializer.ReadObject(stream) as AgentJobRequestMessage;
        
        // Assert
        Assert.NotNull(recoveredMessage);
        Assert.False(recoveredMessage.EnableDebugger, "EnableDebugger should be false when JSON contains 'EnableDebugger': false");
    }

    private static string DoubleQuotify(string text)
    {
        return text.Replace('\'', '"');
    }
}
