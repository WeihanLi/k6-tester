
using System.Text.Json;
using k6_tester.Models;
using k6_tester.Services;

namespace k6_tester;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapPost("/api/k6/script", (K6LoadTestConfig config) =>
        {
            if (!Uri.TryCreate(config.TargetUrl, UriKind.Absolute, out _))
            {
                return Results.BadRequest(new { error = "targetUrl must be an absolute URI." });
            }

            var result = K6ScriptBuilder.BuildScript(config);
            return Results.Ok(result);
        })
        .WithName("GenerateK6Script")
        .WithOpenApi();

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
        })
        .WithName("RunK6Script")
        .WithOpenApi();

        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
