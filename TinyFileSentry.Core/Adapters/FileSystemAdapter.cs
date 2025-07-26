using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Adapters;

public class FileSystemAdapter : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTimeUtc(path);

    public long GetFileSize(string path) => new FileInfo(path).Length;

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = true) 
        => File.Copy(sourcePath, destinationPath, overwrite);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public string[] GetInvalidFileNameChars() => Path.GetInvalidFileNameChars().Select(c => c.ToString()).ToArray();

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
}