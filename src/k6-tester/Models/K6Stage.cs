namespace k6_tester.Models;

public sealed class K6Stage
{
    public string Duration { get; set; } = "30s";

    public int Target { get; set; } = 1;
}
