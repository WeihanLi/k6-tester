using System.Text.Json.Serialization;
using K6Tester.Models;

namespace K6Tester.Services;

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(K6LoadTestConfig))]
[JsonSerializable(typeof(K6RunRequest))]
[JsonSerializable(typeof(K6Stage))]
[JsonSerializable(typeof(K6ScriptResult))]
public partial class AppSerializerContext : JsonSerializerContext;
