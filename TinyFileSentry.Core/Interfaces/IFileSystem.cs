namespace TinyFileSentry.Core.Interfaces;

public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    DateTime GetLastWriteTime(string path);
    long GetFileSize(string path);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = true);
    byte[] ReadAllBytes(string path);
    string[] GetInvalidFileNameChars();
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
}