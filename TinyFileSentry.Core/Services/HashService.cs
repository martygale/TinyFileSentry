using System.Security.Cryptography;
using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Services;

public class HashService : IHashService
{
    private readonly IFileSystem _fileSystem;

    public HashService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash);
    }

    public string ComputeHash(string filePath)
    {
        var data = _fileSystem.ReadAllBytes(filePath);
        return ComputeHash(data);
    }
}