using System.Net.Http.Json;
using System.Text;
using K6Tester.Models;
using K6Tester.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace K6Tester.Tests;

public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProgramTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IK6ScriptBuilder, MockK6ScriptBuilder>();
            }));
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ScriptEndpoint_InvalidTarget_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/script", new K6LoadTestConfig
        {
            TestName = "invalid",
            TargetUrl = "/relative"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestContext.Current.CancellationToken);
        Assert.Equal("targetUrl must be an absolute URI.", body?["error"]);
    }

    [Fact]
    public async Task ScriptEndpoint_WithValidPayload_ReturnsScript()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/script", new K6LoadTestConfig
        {
            TestName = "api-smoke",
            TargetUrl = "https://example.com"
        }, cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<K6ScriptResult>(TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Equal("api-smoke.js", body!.SuggestedFileName);
        Assert.Contains("http.get(url, params)", body.Script);
    }

    [Fact]
    public async Task RunEndpoint_MissingScript_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/run", new K6RunRequest { Script = "" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestContext.Current.CancellationToken);
        Assert.Equal("Test script is required to run k6.", body?["error"]);
    }

    [Fact]
    public async Task RunEndpoint_WithScriptStreamsOutput()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IK6Runner>(_ => MockK6Runner.Success);
            }));
        using var client = factory.CreateClient();

        var payload = new K6RunRequest
        {
            Script = "export default function () { return; }",
            FileName = "api-run.js"
        };

        var response = await client.PostAsJsonAsync("/api/k6/run", payload, cancellationToken: TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("[out] stub stdout line", text);
        Assert.Contains("[exit] k6 exited with code 0.", text);
    }

    [Fact]
    public async Task RunEndpoint_WithErrorsStreamsErrorOutput()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IK6Runner>(_ => MockK6Runner.Error);
            }));
        using var client = factory.CreateClient();

        var payload = new K6RunRequest
        {
            Script = "export default function () { throw new Error('test'); }",
            FileName = "error-run.js"
        };

        var response = await client.PostAsJsonAsync("/api/k6/run", payload, cancellationToken: TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("[err] stub stderr line", text);
        Assert.Contains("[exit] k6 exited with code 1.", text);
    }

    [Fact]
    public async Task ScriptEndpoint_WithEmptyTestName_StillReturnsScript()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/script", new K6LoadTestConfig
        {
            TestName = "",
            TargetUrl = "https://example.com"
        }, cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<K6ScriptResult>(TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.NotEmpty(body!.Script);
    }

    [Fact]
    public async Task ScriptEndpoint_WithNullUri_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/script", new K6LoadTestConfig
        {
            TestName = "test",
            TargetUrl = null!
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScriptEndpoint_WithNonHttpsUri_StillAcceptsRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/script", new K6LoadTestConfig
        {
            TestName = "http-test",
            TargetUrl = "http://example.com"
        }, cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<K6ScriptResult>(TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Contains("http.get(url, params)", body.Script);
    }

    [Fact]
    public async Task RunEndpoint_WithWhitespaceScript_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/run", new K6RunRequest { Script = "   " }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestContext.Current.CancellationToken);
        Assert.Equal("Test script is required to run k6.", body?["error"]);
    }

    [Fact]
    public async Task RunEndpoint_WithNullScript_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/run", new K6RunRequest { Script = null }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestContext.Current.CancellationToken);
        Assert.Equal("Test script is required to run k6.", body?["error"]);
    }

    [Fact]
    public async Task ScriptEndpoint_ResponseContainsExpectedProperties()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/k6/script", new K6LoadTestConfig
        {
            TestName = "full-test",
            TargetUrl = "https://api.example.com",
            Duration = "5m",
            VirtualUsers = 50
        }, cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<K6ScriptResult>(TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.NotEmpty(body!.Script);
        Assert.NotEmpty(body.SuggestedFileName);
        Assert.NotEmpty(body.Command);
        Assert.EndsWith(".js", body.SuggestedFileName);
        Assert.StartsWith("k6 run ", body.Command);
    }
}

class MockK6ScriptBuilder : IK6ScriptBuilder
{
    public K6ScriptResult BuildScript(K6LoadTestConfig config)
    {
        return new K6ScriptResult(
            """http.get(url, params)""",
            $"{config.TestName}.js",
            $"k6 run {config.TestName}.js");
    }
}

class MockK6Runner(string output, string exit, string? errorOutput = null) : IK6Runner
{
    public static IK6Runner Success { get; } = new MockK6Runner("stub stdout line", "k6 exited with code 0.");

    public static IK6Runner Error { get; } = new MockK6Runner("", "k6 exited with code 1.", "stub stderr line");

    public async Task RunAsync(string script, string? fileNameHint, Stream outputStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outputStream);

        await using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);

        if (!string.IsNullOrWhiteSpace(output))
        {
            await writer.WriteLineAsync($"[out] {output}");
        }

        if (!string.IsNullOrWhiteSpace(errorOutput))
        {
            await writer.WriteLineAsync($"[err] {errorOutput}");
        }

        if (!string.IsNullOrEmpty(exit))
        {
            await writer.WriteLineAsync($"[exit] {exit}");
        }

        await writer.FlushAsync();
    }
}
