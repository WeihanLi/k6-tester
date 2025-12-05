using System.Diagnostics;

namespace K6Tester.Services;

public sealed class ProcessRunner : IProcessRunner
{
    public bool TryStart(ProcessStartInfo startInfo, out IProcessHandle? process)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        var systemProcess = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        try
        {
            if (!systemProcess.Start())
            {
                process = null;
                systemProcess.Dispose();
                return false;
            }
        }
        catch
        {
            systemProcess.Dispose();
            throw;
        }

        process = new ProcessHandle(systemProcess);
        return true;
    }

    private sealed class ProcessHandle : IProcessHandle
    {
        private readonly Process _process;

        public ProcessHandle(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
        }

        public StreamReader StandardOutput => _process.StandardOutput;

        public StreamReader StandardError => _process.StandardError;

        public bool HasExited => _process.HasExited;

        public int ExitCode => _process.ExitCode;

        public Task WaitForExitAsync(CancellationToken cancellationToken)
            => _process.WaitForExitAsync(cancellationToken);

        public void Kill(bool entireProcessTree)
            => _process.Kill(entireProcessTree);

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
