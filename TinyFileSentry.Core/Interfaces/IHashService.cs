namespace TinyFileSentry.Core.Interfaces;

public interface IHashService
{
    string ComputeHash(byte[] data);
    string ComputeHash(string filePath);
}