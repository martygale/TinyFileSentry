namespace TinyFileSentry.Core.Interfaces;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StdOut { get; set; } = string.Empty;
    public string StdErr { get; set; } = string.Empty;
}

public interface IProcessRunner
{
    ProcessResult RunCommand(string fileName, string arguments, string? workingDirectory = null);
}