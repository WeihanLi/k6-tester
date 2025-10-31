namespace k6_tester.Models;

public sealed class K6RunRequest
{
    public K6LoadTestConfig? Config { get; set; }

    public string? Script { get; set; }

    public string? FileName { get; set; }
}
