namespace TinyFileSentry.Core.Interfaces;

public interface IPathSanitizer
{
    string SanitizePath(string path);
}