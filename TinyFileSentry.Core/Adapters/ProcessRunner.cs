using System.Diagnostics;
using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Adapters;

public class ProcessRunner : IProcessRunner
{
    public ProcessResult RunCommand(string fileName, string arguments, string? workingDirectory = null)
    {
        using var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        
        if (!string.IsNullOrEmpty(workingDirectory))
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        process.Start();
        
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        
        process.WaitForExit();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StdOut = stdOut,
            StdErr = stdErr
        };
    }
}