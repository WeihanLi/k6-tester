using System.Text.Json.Serialization;
using K6Tester.Models;
using Microsoft.AspNetCore.Mvc;

namespace K6Tester.Services;

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(K6LoadTestConfig))]
[JsonSerializable(typeof(K6OtelOutputConfig))]
[JsonSerializable(typeof(K6OutputConfig))]
[JsonSerializable(typeof(K6RunRequest))]
[JsonSerializable(typeof(K6Stage))]
[JsonSerializable(typeof(K6ScriptResult))]
[JsonSerializable(typeof(ProblemDetails))]
public partial class AppSerializerContext : JsonSerializerContext;
