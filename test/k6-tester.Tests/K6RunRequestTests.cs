using K6Tester.Models;

namespace K6Tester.Tests;

public class K6RunRequestTests
{
    [Fact]
    public void Properties_CanBeAssigned()
    {
        var request = new K6RunRequest
        {
            Script = "console.log('hi');",
            FileName = "test.js"
        };

        Assert.Equal("console.log('hi');", request.Script);
        Assert.Equal("test.js", request.FileName);
    }

    [Fact]
    public void Script_CanBeNull()
    {
        var request = new K6RunRequest
        {
            Script = null
        };

        Assert.Null(request.Script);
    }

    [Fact]
    public void FileName_CanBeNull()
    {
        var request = new K6RunRequest
        {
            Script = "console.log('test');",
            FileName = null
        };

        Assert.Null(request.FileName);
    }

    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        var request = new K6RunRequest();

        Assert.Null(request.Script);
        Assert.Null(request.FileName);
    }

    [Fact]
    public void Properties_CanHandleEmptyStrings()
    {
        var request = new K6RunRequest
        {
            Script = "",
            FileName = ""
        };

        Assert.Equal("", request.Script);
        Assert.Equal("", request.FileName);
    }

    [Fact]
    public void Properties_CanHandleMultilineScript()
    {
        var multilineScript = @"export default function() {
    console.log('line1');
    console.log('line2');
}";
        var request = new K6RunRequest
        {
            Script = multilineScript,
            FileName = "multiline.js"
        };

        Assert.Equal(multilineScript, request.Script);
        Assert.Contains("line1", request.Script);
        Assert.Contains("line2", request.Script);
    }

    [Fact]
    public void Properties_CanHandleSpecialCharactersInScript()
    {
        var request = new K6RunRequest
        {
            Script = "const message = `Hello ${name}!`; // Test 'quotes' and \"more\"",
            FileName = "special.js"
        };

        Assert.Contains("`Hello ${name}!`", request.Script);
        Assert.Contains("'quotes'", request.Script);
    }
}

public class K6LoadTestConfigTests
{
    [Fact]
    public void DefaultValues_AreSet()
    {
        var config = new K6LoadTestConfig();

        Assert.Equal("sample_test", config.TestName);
        Assert.Equal("https://test.k6.io", config.TargetUrl);
        Assert.Equal("GET", config.HttpMethod);
        Assert.Equal("1m", config.Duration);
        Assert.Equal(10, config.VirtualUsers);
        Assert.Equal(1, config.SleepSeconds);
        Assert.True(config.CheckResponse);
        Assert.Empty(config.Stages);
        Assert.Null(config.Headers);
        Assert.Null(config.Payload);
        Assert.Null(config.P95ThresholdMs);
        Assert.Null(config.Tags);
    }

    [Fact]
    public void Properties_CanBeOverridden()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "custom-test",
            TargetUrl = "https://example.com",
            HttpMethod = "POST",
            Duration = "5m",
            VirtualUsers = 50,
            SleepSeconds = 2,
            CheckResponse = false
        };

        Assert.Equal("custom-test", config.TestName);
        Assert.Equal("https://example.com", config.TargetUrl);
        Assert.Equal("POST", config.HttpMethod);
        Assert.Equal("5m", config.Duration);
        Assert.Equal(50, config.VirtualUsers);
        Assert.Equal(2, config.SleepSeconds);
        Assert.False(config.CheckResponse);
    }

    [Fact]
    public void Headers_CanBeSet()
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Authorization"] = "Bearer token"
        };

        var config = new K6LoadTestConfig
        {
            Headers = headers
        };

        Assert.NotNull(config.Headers);
        Assert.Equal(2, config.Headers.Count);
        Assert.Equal("application/json", config.Headers["Content-Type"]);
    }

    [Fact]
    public void Tags_CanBeSet()
    {
        var tags = new Dictionary<string, string>
        {
            ["environment"] = "staging",
            ["team"] = "backend"
        };

        var config = new K6LoadTestConfig
        {
            Tags = tags
        };

        Assert.NotNull(config.Tags);
        Assert.Equal(2, config.Tags.Count);
        Assert.Equal("staging", config.Tags["environment"]);
    }

    [Fact]
    public void Stages_CanBePopulated()
    {
        var config = new K6LoadTestConfig
        {
            Stages = new List<K6Stage>
            {
                new K6Stage { Duration = "30s", Target = 10 },
                new K6Stage { Duration = "1m", Target = 20 }
            }
        };

        Assert.Equal(2, config.Stages.Count);
        Assert.Equal("30s", config.Stages[0].Duration);
        Assert.Equal(10, config.Stages[0].Target);
    }

    [Fact]
    public void Payload_CanBeSet()
    {
        var config = new K6LoadTestConfig
        {
            Payload = "{\"key\":\"value\"}"
        };

        Assert.Equal("{\"key\":\"value\"}", config.Payload);
    }

    [Fact]
    public void P95ThresholdMs_CanBeSet()
    {
        var config = new K6LoadTestConfig
        {
            P95ThresholdMs = 500
        };

        Assert.Equal(500, config.P95ThresholdMs);
    }
}
