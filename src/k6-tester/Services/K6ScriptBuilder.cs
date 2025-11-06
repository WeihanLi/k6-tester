using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using K6Tester.Models;

namespace K6Tester.Services;

public static class K6ScriptBuilder
{
    private static readonly Regex FileNameSanitizer = new("[^a-zA-Z0-9_-]+", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static K6ScriptResult BuildScript(K6LoadTestConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var method = NormalizeMethod(config.HttpMethod);
        var scenarioName = EnsureScenarioName(config.TestName);
        var fileName = $"{scenarioName}.js";

        var script = GenerateScript(config, method, scenarioName);
        var command = $"k6 run {fileName}";

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
            var tagsJson = JsonSerializer.Serialize(config.Tags, JsonOptions);
            builder.AppendLine($"      tags: {tagsJson},");
        }

        builder.AppendLine("    },");
        builder.AppendLine("  },");

        if (config.P95ThresholdMs is { } thresholdMs && thresholdMs > 0)
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
            var json = JsonSerializer.Serialize(headers, JsonOptions);
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
}
