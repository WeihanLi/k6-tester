namespace k6_tester.Models;

public sealed record K6ScriptResult(string Script, string SuggestedFileName, string Command);
