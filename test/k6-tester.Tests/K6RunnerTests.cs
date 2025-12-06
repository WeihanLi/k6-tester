using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using K6Tester.Services;

namespace K6Tester.Tests;

public class K6RunnerTests
{
    [Fact]
    public async Task RunAsync_WithProcessOutputs_StreamsStdoutAndStderr()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new TestProcessRunner(
            stdout: new[] { "stub stdout line" },
            stderr: new[] { "stub stderr line" }));

        await runner.RunAsync("console.log('ok');", "sample-script.js", output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[out] stub stdout line", text);
        Assert.Contains("[err] stub stderr line", text);
        Assert.Contains("[exit] k6 exited with code 0.", text);
    }

    [Fact]
    public async Task RunAsync_WhenCancelledBeforeStart_WritesCancellationMessage()
    {
        using var output = new MemoryStream();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var runner = new K6Runner(new TestProcessRunner(Array.Empty<string>(), Array.Empty<string>()));

        await runner.RunAsync("console.log('cancel');", "cancel.js", output, cts.Token);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[cancelled] Execution cancelled.", text);
    }

    [Fact]
    public async Task RunAsync_WhenProcessCannotStart_WritesErrorMessage()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new FailingProcessRunner());

        await runner.RunAsync("console.log('fail');", "fail.js", output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[error] Failed to start k6 process.", text);
    }

    [Fact]
    public async Task RunAsync_WhenProcessExitsWithNonZeroCode_ReportsCorrectExitCode()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new TestProcessRunner(
            stdout: new[] { "running test" },
            stderr: new[] { "test failed" },
            exitCode: 1));

        await runner.RunAsync("console.log('test');", "test.js", output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[out] running test", text);
        Assert.Contains("[err] test failed", text);
        Assert.Contains("[exit] k6 exited with code 1.", text);
    }

    [Fact]
    public async Task RunAsync_WithMultipleStdoutLines_StreamsAllLines()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new TestProcessRunner(
            stdout: new[] { "line 1", "line 2", "line 3" },
            stderr: Array.Empty<string>()));

        await runner.RunAsync("console.log('test');", "test.js", output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[out] line 1", text);
        Assert.Contains("[out] line 2", text);
        Assert.Contains("[out] line 3", text);
    }

    [Fact]
    public async Task RunAsync_WithMultipleStderrLines_StreamsAllLines()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new TestProcessRunner(
            stdout: Array.Empty<string>(),
            stderr: new[] { "error 1", "error 2" }));

        await runner.RunAsync("console.log('test');", "test.js", output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[err] error 1", text);
        Assert.Contains("[err] error 2", text);
    }

    [Fact]
    public async Task RunAsync_WithEmptyFileName_UsesDefaultFileName()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new TestProcessRunner(
            stdout: new[] { "test output" },
            stderr: Array.Empty<string>()));

        await runner.RunAsync("console.log('test');", "", output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[exit] k6 exited with code 0.", text);
    }

    [Fact]
    public async Task RunAsync_WithNullFileName_UsesDefaultFileName()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new TestProcessRunner(
            stdout: new[] { "test output" },
            stderr: Array.Empty<string>()));

        await runner.RunAsync("console.log('test');", null, output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[exit] k6 exited with code 0.", text);
    }

    [Fact]
    public async Task RunAsync_WithNoOutput_OnlyWritesExitMessage()
    {
        using var output = new MemoryStream();
        var runner = new K6Runner(new TestProcessRunner(
            stdout: Array.Empty<string>(),
            stderr: Array.Empty<string>()));

        await runner.RunAsync("console.log('test');", "test.js", output, TestContext.Current.CancellationToken);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.DoesNotContain("[out]", text);
        Assert.DoesNotContain("[err]", text);
        Assert.Contains("[exit] k6 exited with code 0.", text);
    }

    private sealed class FailingProcessRunner : IProcessRunner
    {
        public bool TryStart(ProcessStartInfo startInfo, out IProcessHandle? process)
        {
            process = null;
            return false;
        }
    }

    private sealed class TestProcessRunner : IProcessRunner
    {
        private readonly IReadOnlyList<string> _stdout;
        private readonly IReadOnlyList<string> _stderr;
        private readonly int _exitCode;

        public TestProcessRunner(IEnumerable<string> stdout, IEnumerable<string> stderr, int exitCode = 0)
        {
            _stdout = stdout.ToList();
            _stderr = stderr.ToList();
            _exitCode = exitCode;
        }

        public bool TryStart(ProcessStartInfo startInfo, out IProcessHandle? process)
        {
            process = new TestProcessHandle(_stdout, _stderr, _exitCode);
            return true;
        }
    }

    private sealed class TestProcessHandle : IProcessHandle
    {
        private readonly StreamReader _stdout;
        private readonly StreamReader _stderr;

        public TestProcessHandle(IEnumerable<string> stdout, IEnumerable<string> stderr, int exitCode)
        {
            _stdout = CreateReader(stdout);
            _stderr = CreateReader(stderr);
            ExitCode = exitCode;
        }

        public StreamReader StandardOutput => _stdout;
        public StreamReader StandardError => _stderr;
        public bool HasExited => true;
        public int ExitCode { get; }

        public Task WaitForExitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Kill(bool entireProcessTree)
        {
        }

        public void Dispose()
        {
            _stdout.Dispose();
            _stderr.Dispose();
        }

        private static StreamReader CreateReader(IEnumerable<string> lines)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }

                writer.Flush();
            }

            stream.Position = 0;
            return new StreamReader(stream, Encoding.UTF8);
        }
    }
}
