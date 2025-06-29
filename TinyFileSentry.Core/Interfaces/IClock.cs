namespace TinyFileSentry.Core.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}