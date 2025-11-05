using System.Numerics;

namespace Core.DebugTools.Data;

public sealed record ConsoleCmdRequest(
    string Command,
    string? Arg1 = null,
    string? Arg2 = null,
    ConsoleCmdPayload? Payload = null
);

public sealed record ConsoleCmdPayload(
    int TargetId,
    int IntData,
    Vector4 Data1,
    Vector4 Data2
);