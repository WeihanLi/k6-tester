using K6Tester.Models;
using K6Tester.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IK6Runner, K6Runner>();
builder.Services.AddSingleton<IK6ScriptBuilder, K6ScriptBuilder>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(AppSerializerContext.Default);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health/live", () => Results.Ok())
    .ShortCircuit().DisableHttpMetrics();

app.MapPost("/api/k6/script", (K6LoadTestConfig config, [FromServices] IK6ScriptBuilder scriptBuilder) =>
{
    if (!Uri.TryCreate(config.TargetUrl, UriKind.Absolute, out _))
    {
        return Results.Problem(detail: "targetUrl must be an absolute URI.", statusCode: 400);
    }

    var result = scriptBuilder.BuildScript(config);
    return Results.Ok(result);
});

app.MapPost("/api/k6/run", (HttpContext httpContext, K6RunRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request?.Script))
    {
        return Results.Problem(detail: "Test script is required to run k6.", statusCode: 400);
    }

    if (request.Output is not null && string.IsNullOrWhiteSpace(request.Output.Type))
    {
        return Results.Problem(detail: "Output 'type' is required when an output configuration is provided.", statusCode: 400);
    }

    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Pragma = "no-cache";

    var scriptToRun = request.Script;
    var fileName = string.IsNullOrWhiteSpace(request.FileName)
        ? "k6-script.js"
        : request.FileName;

    return Results.Stream(async stream =>
    {
        await httpContext.RequestServices.GetRequiredService<IK6Runner>()
            .RunAsync(scriptToRun, fileName, stream, httpContext.RequestAborted, request.Output);
    }, "text/plain; charset=utf-8");
});

app.MapFallbackToFile("index.html");

await app.RunAsync();
