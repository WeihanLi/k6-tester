namespace K6Tester.Models;

/// <summary>
/// Configures a k6 output destination (e.g. <c>--out opentelemetry</c>, <c>--out influxdb=http://...</c>).
/// </summary>
public sealed class K6OutputConfig
{
    /// <summary>
    /// The output type name as understood by k6, e.g. "opentelemetry", "influxdb", "json", "csv", "cloud".
    /// Maps directly to the value passed to <c>--out</c>.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Optional value appended to the type: <c>--out {Type}={Url}</c>.
    /// Used by outputs such as influxdb (<c>http://localhost:8086/k6</c>), json (<c>/path/output.json</c>), or csv.
    /// Leave null/empty for outputs that are configured solely via environment variables (e.g. opentelemetry).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// OpenTelemetry-specific options. Only used when <see cref="Type"/> is "opentelemetry".
    /// These are translated into <c>K6_OTEL_*</c> environment variables at run time.
    /// </summary>
    public K6OtelOutputConfig? OpenTelemetry { get; set; }
}
