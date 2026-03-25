namespace K6Tester.Models;

public sealed class K6OtelOutputConfig
{
    /// <summary>The exporter protocol: "grpc" or "http". Defaults to "grpc".</summary>
    public string Protocol { get; set; } = "grpc";

    /// <summary>The OTEL collector endpoint. For gRPC: host:port (e.g. localhost:4317); for HTTP: host:port (e.g. localhost:4318).</summary>
    public string? Endpoint { get; set; }

    /// <summary>Disable TLS when connecting to the OTEL collector.</summary>
    public bool Insecure { get; set; }

    /// <summary>Service name reported to the OTEL collector. Defaults to "k6".</summary>
    public string? ServiceName { get; set; }

    /// <summary>Metric name prefix for all exported metrics. Defaults to "k6.".</summary>
    public string? MetricPrefix { get; set; }

    /// <summary>How often to flush metrics, e.g. "1s". Defaults to "1s".</summary>
    public string? FlushInterval { get; set; }

    /// <summary>Additional HTTP headers for HTTP exporter (key=value pairs separated by commas, e.g. "x-api-key=secret").</summary>
    public string? Headers { get; set; }
}
