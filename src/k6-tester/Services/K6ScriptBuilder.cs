using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using K6Tester.Models;

namespace K6Tester.Services;

public interface IK6ScriptBuilder
{
    K6ScriptResult BuildScript(K6LoadTestConfig config);
}

public partial class K6ScriptBuilder : IK6ScriptBuilder
{
    public K6ScriptResult BuildScript(K6LoadTestConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var method = NormalizeMethod(config.HttpMethod);
        var scenarioName = EnsureScenarioName(config.TestName);
        var fileName = $"{scenarioName}.js";

        var script = GenerateScript(config, method, scenarioName);
        var command = BuildCommand(fileName, config.OtelOutput);

        return new K6ScriptResult(script, fileName, command);
    }

    private static string GenerateScript(K6LoadTestConfig config, string method, string scenarioName)
    {
        var builder = new StringBuilder();

        builder.AppendLine("import http from 'k6/http';");
        builder.AppendLine("import { check, sleep } from 'k6';");
        builder.AppendLine();
        builder.AppendLine("export const options = {");
        builder.AppendLine("  scenarios: {");
        builder.AppendLine($"    '{scenarioName}': {{");

        if (config.Stages is { Count: > 0 })
        {
            builder.AppendLine("      executor: 'ramping-vus',");
            builder.AppendLine("      stages: [");
            foreach (var stage in config.Stages)
            {
                var duration = string.IsNullOrWhiteSpace(stage.Duration) ? "30s" : stage.Duration.Trim();
                var target = Math.Max(0, stage.Target);
                builder.AppendLine($"        {{ duration: '{duration}', target: {target} }},");
            }

            builder.AppendLine("      ],");
        }
        else
        {
            var duration = string.IsNullOrWhiteSpace(config.Duration) ? "1m" : config.Duration.Trim();
            var vus = Math.Max(1, config.VirtualUsers);
            builder.AppendLine("      executor: 'constant-vus',");
            builder.AppendLine($"      vus: {vus},");
            builder.AppendLine($"      duration: '{duration}',");
        }

        if (config.Tags is { Count: > 0 })
        {
            var tagsJson = JsonSerializer.Serialize(config.Tags, AppSerializerContext.Default.DictionaryStringString);
            builder.AppendLine($"      tags: {tagsJson},");
        }

        builder.AppendLine("    },");
        builder.AppendLine("  },");

        if (config.P95ThresholdMs is { } thresholdMs and > 0)
        {
            builder.AppendLine("  thresholds: {");
            builder.AppendLine($"    http_req_duration: ['p(95)<{thresholdMs}'],");
            builder.AppendLine("  },");
        }

        builder.AppendLine("};");
        builder.AppendLine();

        var url = string.IsNullOrWhiteSpace(config.TargetUrl)
            ? "https://k6.io"
            : config.TargetUrl.Trim();

        builder.AppendLine($"const url = '{EscapeSingleQuotes(url)}';");
        builder.AppendLine();
        builder.AppendLine("export default function () {");

        foreach (var line in BuildHeadersBlock(config.Headers))
        {
            builder.AppendLine(line);
        }

        builder.AppendLine();

        if (RequiresPayload(method))
        {
            foreach (var line in BuildPayloadBlock(config.Payload))
            {
                builder.AppendLine(line);
            }

            builder.AppendLine();
        }

        builder.AppendLine($"  const response = {BuildRequestLine(method)};");

        if (config.CheckResponse)
        {
            builder.AppendLine("  check(response, {");
            builder.AppendLine("    'status is 200': (r) => r.status === 200,");
            builder.AppendLine("  });");
        }

        var sleepSeconds = Math.Max(0, config.SleepSeconds);
        if (sleepSeconds > 0)
        {
            builder.AppendLine($"  sleep({sleepSeconds});");
        }
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static IEnumerable<string> BuildHeadersBlock(Dictionary<string, string>? headers)
    {
        if (headers is { Count: > 0 })
        {
            var json = JsonSerializer.Serialize(headers, AppSerializerContext.Default.DictionaryStringString);
            yield return $"  const params = {{ headers: {json} }};";
        }
        else
        {
            yield return "  const params = {};";
        }
    }

    private static IEnumerable<string> BuildPayloadBlock(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            yield return "  const payload = ''; // Provide a payload when using POST, PUT or PATCH";
        }
        else
        {
            var normalized = payload.Replace("\r\n", "\n");
            var escaped = EscapeBackticks(normalized);
            yield return "  const payload = `";
            foreach (var line in escaped.Split('\n'))
            {
                yield return $"  {line}";
            }

            yield return "  `;";
        }
    }

    private static string BuildRequestLine(string method) =>
        method switch
        {
            "POST" or "PUT" or "PATCH" => $"http.{method.ToLowerInvariant()}(url, payload, params)",
            "DELETE" => "http.del(url, params)",
            _ => "http.get(url, params)"
        };

    private static bool RequiresPayload(string method) =>
        method is "POST" or "PUT" or "PATCH";

    private static string NormalizeMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return "GET";
        }

        var normalized = method.Trim().ToUpperInvariant();
        return normalized is "GET" or "POST" or "PUT" or "PATCH" or "DELETE"
            ? normalized
            : "GET";
    }

    private static string EnsureScenarioName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "k6_scenario";
        }

        var sanitized = FileNameSanitizer.Replace(name.Trim(), "_").Trim('_');

        if (string.IsNullOrEmpty(sanitized))
        {
            return "k6_scenario";
        }

        if (char.IsDigit(sanitized[0]))
        {
            sanitized = $"s_{sanitized}";
        }

        return sanitized.ToLowerInvariant();
    }

    private static string EscapeSingleQuotes(string value) => value.Replace("'", "\\'");

    private static string EscapeBackticks(string value) => value.Replace("`", "\\`");

    private static string BuildCommand(string fileName, K6OtelOutputConfig? otelOutput)
    {
        if (otelOutput is null)
        {
            return $"k6 run {fileName}";
        }

        var envVars = BuildOtelEnvironmentVariables(otelOutput);
        var envPrefix = envVars.Count > 0
            ? string.Join(" ", envVars.Select(kv => $"{kv.Key}={kv.Value}")) + " "
            : string.Empty;

        return $"{envPrefix}k6 run --out opentelemetry {fileName}";
    }

    internal static Dictionary<string, string> BuildOtelEnvironmentVariables(K6OtelOutputConfig otelOutput)
    {
        var env = new Dictionary<string, string>();
        var isHttp = string.Equals(otelOutput.Protocol, "http", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(otelOutput.Endpoint))
        {
            var key = isHttp
                ? "K6_OTEL_HTTP_EXPORTER_ENDPOINT"
                : "K6_OTEL_GRPC_EXPORTER_ENDPOINT";
            env[key] = otelOutput.Endpoint.Trim();
        }

        if (otelOutput.Insecure)
        {
            var key = isHttp
                ? "K6_OTEL_HTTP_EXPORTER_INSECURE"
                : "K6_OTEL_GRPC_EXPORTER_INSECURE";
            env[key] = "true";
        }

        if (!string.IsNullOrWhiteSpace(otelOutput.Headers) && isHttp)
        {
            env["K6_OTEL_HTTP_EXPORTER_HEADERS"] = otelOutput.Headers.Trim();
        }

        if (!string.IsNullOrWhiteSpace(otelOutput.ServiceName))
        {
            env["K6_OTEL_SERVICE_NAME"] = otelOutput.ServiceName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(otelOutput.MetricPrefix))
        {
            env["K6_OTEL_METRIC_PREFIX"] = otelOutput.MetricPrefix.Trim();
        }

        if (!string.IsNullOrWhiteSpace(otelOutput.FlushInterval))
        {
            env["K6_OTEL_FLUSH_INTERVAL"] = otelOutput.FlushInterval.Trim();
        }

        return env;
    }

    [GeneratedRegex("[^a-zA-Z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex FileNameSanitizer { get; }
}
