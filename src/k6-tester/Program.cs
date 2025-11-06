using System.Text.Json;
using K6Tester.Models;
using K6Tester.Services;

var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health/live", () => Results.Ok())
    .ShortCircuit();

app.MapPost("/api/k6/script", (K6LoadTestConfig config) =>
{
    if (!Uri.TryCreate(config.TargetUrl, UriKind.Absolute, out _))
    {
        return Results.BadRequest(new { error = "targetUrl must be an absolute URI." });
    }

    var result = K6ScriptBuilder.BuildScript(config);
    return Results.Ok(result);
});

app.MapPost("/api/k6/run", (HttpContext httpContext, K6RunRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request?.Script))
    {
        return Results.BadRequest(new { error = "Test script is required to run k6." });
    }

    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Pragma = "no-cache";
    httpContext.Response.Headers["X-Accel-Buffering"] = "no";

    var cancellationToken = httpContext.RequestAborted;
    var scriptToRun = request.Script;
    var fileName = string.IsNullOrWhiteSpace(request.FileName)
        ? "k6-script.js"
        : request.FileName;

    return Results.Stream(async stream =>
    {
        await K6Runner.RunAsync(scriptToRun, fileName, stream, cancellationToken);
    }, "text/plain; charset=utf-8");
});

app.MapFallbackToFile("index.html");

await app.RunAsync();
