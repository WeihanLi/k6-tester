namespace K6Tester.Models;

/// <summary>
/// Configures the k6 Prometheus remote write output (<c>--out experimental-prometheus-rw</c>).
/// These options are translated into <c>K6_PROMETHEUS_RW_*</c> environment variables at run time.
/// </summary>
public sealed class K6PrometheusRwOutputConfig
{
    /// <summary>The Prometheus remote write endpoint URL, e.g. "http://localhost:9090/api/v1/write".</summary>
    public string? ServerUrl { get; set; }

    /// <summary>Username for HTTP basic authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Password for HTTP basic authentication.</summary>
    public string? Password { get; set; }

    /// <summary>Additional HTTP headers (key:value pairs separated by commas, e.g. "X-Header:value").</summary>
    public string? Headers { get; set; }

    /// <summary>Comma-separated list of stats to flush for Trend metrics, e.g. "p(95),p(99),min,max".</summary>
    public string? TrendStats { get; set; }

    /// <summary>How often to flush metrics, e.g. "5s".</summary>
    public string? PushInterval { get; set; }

    /// <summary>Skip TLS certificate verification when connecting to the endpoint.</summary>
    public bool InsecureSkipTlsVerify { get; set; }
}
