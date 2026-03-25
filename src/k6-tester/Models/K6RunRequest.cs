namespace K6Tester.Models;

public sealed class K6RunRequest
{
    public string? Script { get; set; }

    public string? FileName { get; set; }

    public K6OutputConfig? Output { get; set; }
}
