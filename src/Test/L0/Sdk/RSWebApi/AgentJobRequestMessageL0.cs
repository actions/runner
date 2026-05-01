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

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyDebuggerTunnelDeserialization_WithTunnel()
    {
        // Arrange
        var serializer = new DataContractJsonSerializer(typeof(AgentJobRequestMessage), new DataContractJsonSerializerSettings
        {
            KnownTypes = new[] { typeof(DebuggerTunnelInfo) }
        });
        string json = DoubleQuotify(
            "{'EnableDebugger': true, 'DebuggerTunnel': {'TunnelId': 'tun-123', 'ClusterId': 'use2', 'HostToken': 'tok-abc', 'Port': 4711}}");

        // Act
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(json));
        stream.Position = 0;
        var recoveredMessage = serializer.ReadObject(stream) as AgentJobRequestMessage;

        // Assert
        Assert.NotNull(recoveredMessage);
        Assert.True(recoveredMessage.EnableDebugger);
        Assert.NotNull(recoveredMessage.DebuggerTunnel);
        Assert.Equal("tun-123", recoveredMessage.DebuggerTunnel.TunnelId);
        Assert.Equal("use2", recoveredMessage.DebuggerTunnel.ClusterId);
        Assert.Equal("tok-abc", recoveredMessage.DebuggerTunnel.HostToken);
        Assert.Equal(4711, recoveredMessage.DebuggerTunnel.Port);
    }

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyDebuggerTunnelDeserialization_WithoutTunnel()
    {
        // Arrange
        var serializer = new DataContractJsonSerializer(typeof(AgentJobRequestMessage));
        string json = DoubleQuotify("{'EnableDebugger': true}");

        // Act
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(json));
        stream.Position = 0;
        var recoveredMessage = serializer.ReadObject(stream) as AgentJobRequestMessage;

        // Assert
        Assert.NotNull(recoveredMessage);
        Assert.True(recoveredMessage.EnableDebugger);
        Assert.Null(recoveredMessage.DebuggerTunnel);
    }

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyActionsDependenciesDeserialization_WithDependencies()
    {
        // Arrange
        var serializer = new DataContractJsonSerializer(typeof(AgentJobRequestMessage));
        string json = DoubleQuotify("{'dependencies': ['actions/checkout@v4:sha256-abc123', 'actions/setup-node@v4:sha256-def456']}");

        // Act
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(json));
        stream.Position = 0;
        var recoveredMessage = serializer.ReadObject(stream) as AgentJobRequestMessage;

        // Assert
        Assert.NotNull(recoveredMessage);
        Assert.Equal(2, recoveredMessage.ActionsDependencies.Count);
        Assert.Equal("actions/checkout@v4:sha256-abc123", recoveredMessage.ActionsDependencies[0]);
        Assert.Equal("actions/setup-node@v4:sha256-def456", recoveredMessage.ActionsDependencies[1]);
    }

    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "Common")]
    public void VerifyActionsDependenciesDeserialization_DefaultsToEmpty()
    {
        // Arrange
        var serializer = new DataContractJsonSerializer(typeof(AgentJobRequestMessage));
        string json = DoubleQuotify("{'messageType': 'PipelineAgentJobRequest'}");

        // Act
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(json));
        stream.Position = 0;
        var recoveredMessage = serializer.ReadObject(stream) as AgentJobRequestMessage;

        // Assert
        Assert.NotNull(recoveredMessage);
        Assert.Empty(recoveredMessage.ActionsDependencies);
    }

    private static string DoubleQuotify(string text)
    {
        return text.Replace('\'', '"');
    }
}
