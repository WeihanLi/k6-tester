namespace k6_tester.Models;

public sealed class K6RunRequest
{
    public string? Script { get; set; }

    public string? FileName { get; set; }
}
