using K6Tester.Models;
using K6Tester.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace K6Tester.Tests;

public class K6ScriptBuilderTests
{
    private readonly IK6ScriptBuilder _k6ScriptBuilder = new K6ScriptBuilder();

    [Fact]
    public void BuildScript_WhenNoStagesConfigured_UsesConstantVuExecutor()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "Smoke",
            TargetUrl = "https://example.com",
            Duration = "2m",
            VirtualUsers = 25
        };

        var result = _k6ScriptBuilder.BuildScript(config);

        Assert.Equal("smoke.js", result.SuggestedFileName);
        Assert.Contains("executor: 'constant-vus'", result.Script);
        Assert.Contains("vus: 25", result.Script);
        Assert.Contains("duration: '2m'", result.Script);
        Assert.Contains("http.get(url, params)", result.Script);
    }

    [Fact]
    public void BuildScript_WhenStagesProvided_UsesRampingVuExecutor()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "Ramp",
            TargetUrl = "https://example.com",
            Stages =
            [
                new K6Stage { Duration = "30s", Target = 5 },
                new K6Stage { Duration = "1m", Target = 20 },
            ]
        };

        var result = _k6ScriptBuilder.BuildScript(config);

        Assert.Contains("executor: 'ramping-vus'", result.Script);
        Assert.Contains("{ duration: '30s', target: 5 }", result.Script);
        Assert.Contains("{ duration: '1m', target: 20 }", result.Script);
    }

    [Fact]
    public void BuildScript_NormalizesScenarioNameForSuggestedFile()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "123 Homepage Load",
            TargetUrl = "https://example.com"
        };

        var result = _k6ScriptBuilder.BuildScript(config);

        Assert.Equal("s_123_homepage_load.js", result.SuggestedFileName);
        Assert.Contains("'s_123_homepage_load'", result.Script);
        Assert.Equal("k6 run s_123_homepage_load.js", result.Command);
    }

    [Fact]
    public void BuildScript_WithInvalidMethod_FallsBackToGet()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "InvalidMethod",
            TargetUrl = "https://example.com",
            HttpMethod = "TRACE"
        };

        var result = _k6ScriptBuilder.BuildScript(config);

        Assert.Contains("http.get(url, params)", result.Script);
    }

    [Fact]
    public void BuildScript_WithHeadersTagsAndThresholds_IncludesAllSections()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "Full",
            TargetUrl = "https://example.com/api",
            HttpMethod = "POST",
            Payload = "{\"foo\":\"bar\"}",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Tags = new Dictionary<string, string>
            {
                ["environment"] = "test"
            },
            P95ThresholdMs = 500,
            Stages =
            [
                new K6Stage { Duration = "30s", Target = 10 }
            ]
        };

        var result = _k6ScriptBuilder.BuildScript(config);

        Assert.Contains("\"environment\":\"test\"", result.Script);
        Assert.Contains("http_req_duration: ['p(95)<500']", result.Script);
        Assert.Contains("http.post(url, payload, params)", result.Script);
        Assert.Contains("const payload = `", result.Script);
        Assert.Contains("const params = { headers:", result.Script);
    }

    [Fact]
    public void BuildScript_WhenConfigIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _k6ScriptBuilder.BuildScript(null!));
    }

    [Fact]
    public void BuildScript_WithStagesMissingData_NormalizesDurationAndTarget()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "NormalizeStage",
            TargetUrl = "https://example.com",
            Stages =
            [
                new K6Stage { Duration = " ", Target = -5 },
                new K6Stage { Duration = " 45s ", Target = 10 }
            ]
        };

        var result = _k6ScriptBuilder.BuildScript(config).Script;

        Assert.Contains("{ duration: '30s', target: 0 }", result);
        Assert.Contains("{ duration: '45s', target: 10 }", result);
    }

    [Fact]
    public void BuildScript_WhenNoHeadersProvided_InitializesEmptyParams()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "NoHeaders",
            TargetUrl = "https://example.com"
        };

        var result = _k6ScriptBuilder.BuildScript(config).Script;

        Assert.Contains("const params = {};", result);
    }

    [Fact]
    public void BuildScript_WithPostWithoutPayload_AddsPlaceholderAndPostCall()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "PostPlaceholder",
            TargetUrl = "https://example.com",
            HttpMethod = "post",
            Payload = "",
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };

        var result = _k6ScriptBuilder.BuildScript(config).Script;

        Assert.Contains("const payload = ''; // Provide a payload when using POST, PUT or PATCH", result);
        Assert.Contains("http.post(url, payload, params)", result);
    }

    [Fact]
    public void BuildScript_WhenSleepSecondsPositive_AddsSleepAndSkipsCheckWhenDisabled()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "Sleepy",
            TargetUrl = "https://example.com",
            SleepSeconds = 3,
            CheckResponse = false
        };

        var result = _k6ScriptBuilder.BuildScript(config).Script;

        Assert.Contains("sleep(3);", result);
        Assert.DoesNotContain("status is 200", result);
    }

    [Fact]
    public void BuildScript_EscapesSingleQuotesInTargetUrl()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "Escapes",
            TargetUrl = " https://api.example.com/product?id=42&name=Bob's Burger "
        };

        var result = _k6ScriptBuilder.BuildScript(config).Script;

        Assert.Contains("const url = 'https://api.example.com/product?id=42&name=Bob\\'s Burger';", result);
    }

    [Fact]
    public void BuildScript_WithMultilinePayload_EscapesBackticks()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "PayloadEscape",
            TargetUrl = "https://example.com",
            HttpMethod = "POST",
            Payload = "line1\r\nline`2"
        };

        var script = _k6ScriptBuilder.BuildScript(config).Script;

        Assert.Contains("line1", script);
        Assert.Contains("line\\`2", script);
        Assert.Contains("const payload = `", script);
    }

    [Fact]
    public void BuildScript_WhenTargetUrlMissing_FallsBackToDefault()
    {
        var config = new K6LoadTestConfig
        {
            TestName = "FallbackUrl",
            TargetUrl = ""
        };

        var script = _k6ScriptBuilder.BuildScript(config).Script;

        Assert.Contains("const url = 'https://k6.io';", script);
    }
}
