using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace K6Tester.Services;

public static class K6Runner
{
    public static async Task RunAsync(string script, string? fileNameHint, Stream outputStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outputStream);

        if (string.IsNullOrWhiteSpace(script))
        {
            throw new ArgumentException("Script cannot be null or whitespace.", nameof(script));
        }

        var tempFileName = BuildTempFileName(fileNameHint);
        var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

        try
        {
            await File.WriteAllTextAsync(tempFilePath, script, Encoding.UTF8, cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = "k6",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add(tempFilePath);

            using var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            if (!process.Start())
            {
                await WriteLineAsync(outputStream, "[error] Failed to start k6 process.", cancellationToken);
                return;
            }

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore failures while trying to kill the process on cancellation.
                }
            });

            var outputTask = PipeStreamAsync(process.StandardOutput, "[out]", outputStream, cancellationToken);
            var errorTask = PipeStreamAsync(process.StandardError, "[err]", outputStream, cancellationToken);
            var waitForExitTask = process.WaitForExitAsync(cancellationToken);

            await Task.WhenAll(waitForExitTask, outputTask, errorTask);

            await WriteLineAsync(outputStream, $"[exit] k6 exited with code {process.ExitCode}.", cancellationToken);
        }
        catch (Win32Exception)
        {
            await WriteLineAsync(outputStream, "[error] k6 executable not found in PATH. Install k6 to enable running scripts.", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await WriteLineAsync(outputStream, "[cancelled] Execution cancelled.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            await WriteLineAsync(outputStream, $"[error] {ex.Message}", cancellationToken);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }
    }

    private static string BuildTempFileName(string? fileNameHint)
    {
        var baseName = string.IsNullOrWhiteSpace(fileNameHint)
            ? "k6-script"
            : Path.GetFileNameWithoutExtension(fileNameHint);

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "k6-script";
        }

        var uniqueSuffix = Path.GetRandomFileName().Replace(".", string.Empty);
        return $"{baseName}_{uniqueSuffix}.js";
    }

    private static async Task PipeStreamAsync(StreamReader reader, string prefix, Stream destination, CancellationToken cancellationToken)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                await WriteLineAsync(destination, $"{prefix} {line}", cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation while reading.
        }
    }

    private static async Task WriteLineAsync(Stream destination, string message, CancellationToken cancellationToken)
    {
        var formatted = $"{message}{Environment.NewLine}";
        var bytes = Encoding.UTF8.GetBytes(formatted);
        try
        {
            await destination.WriteAsync(bytes, cancellationToken);
            await destination.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation while writing.
        }
        catch (ObjectDisposedException)
        {
            // Ignore if the response stream is no longer available.
        }
        catch (IOException)
        {
            // Ignore write failures caused by disconnected clients.
        }
    }
}
