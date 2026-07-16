using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.Logging;

internal struct LogEntry(RangeU16 handle)
{
    public const byte TimestampOffset = 13;
    public RangeU16 Handle = handle;
    public LogScope Scope;
    public LogLevel Level;
}