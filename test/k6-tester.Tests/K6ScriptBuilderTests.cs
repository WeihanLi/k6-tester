using k6_tester.Models;
using k6_tester.Services;

namespace k6_tester.Tests;

public class K6ScriptBuilderTests
{
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

        var result = K6ScriptBuilder.BuildScript(config);

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

        var result = K6ScriptBuilder.BuildScript(config);

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

        var result = K6ScriptBuilder.BuildScript(config);

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

        var result = K6ScriptBuilder.BuildScript(config);

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

        var result = K6ScriptBuilder.BuildScript(config);

        Assert.Contains("\"environment\":\"test\"", result.Script);
        Assert.Contains("http_req_duration: ['p(95)<500']", result.Script);
        Assert.Contains("http.post(url, payload, params)", result.Script);
        Assert.Contains("const payload = `", result.Script);
        Assert.Contains("const params = { headers:", result.Script);
    }
}
