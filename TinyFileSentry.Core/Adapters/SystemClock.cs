using TinyFileSentry.Core.Interfaces;

namespace TinyFileSentry.Core.Adapters;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}