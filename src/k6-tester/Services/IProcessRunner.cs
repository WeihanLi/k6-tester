using System.Diagnostics;

namespace K6Tester.Services;

public interface IProcessRunner
{
    bool TryStart(ProcessStartInfo startInfo, out IProcessHandle? process);
}

public interface IProcessHandle : IDisposable
{
    StreamReader StandardOutput { get; }
    StreamReader StandardError { get; }
    bool HasExited { get; }
    int ExitCode { get; }
    Task WaitForExitAsync(CancellationToken cancellationToken);
    void Kill(bool entireProcessTree);
}
