namespace K6Tester.Models;

public sealed class K6LoadTestConfig
{
    public string TestName { get; set; } = "sample_test";

    public string TargetUrl { get; set; } = "https://test.k6.io";

    public string HttpMethod { get; set; } = "GET";

    public string Duration { get; set; } = "1m";

    public int VirtualUsers { get; set; } = 10;

    public List<K6Stage> Stages { get; set; } = [];

    public int SleepSeconds { get; set; } = 1;

    public Dictionary<string, string>? Headers { get; set; }

    public string? Payload { get; set; }

    public int? P95ThresholdMs { get; set; }

    public Dictionary<string, string>? Tags { get; set; }

    public bool CheckResponse { get; set; } = true;
}
