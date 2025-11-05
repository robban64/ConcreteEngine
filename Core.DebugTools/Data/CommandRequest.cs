using System.Numerics;

namespace Core.DebugTools.Data;

public sealed record ConsoleCommandRequest(
    string Command,
    string? Action = null,
    string? Args = null,
    IConsoleCmdPayload? Payload = null
);

public interface IConsoleCmdPayload;

public sealed record GenericCmdPayload(
    int TargetId,
    int IntArg = 0,
    float FloatArg = 0
) : IConsoleCmdPayload;

public sealed class TransformCmdPayload(int entityId, in EntityEditorTransform transform) : IConsoleCmdPayload
{
    public int EntityId { get; init; } = entityId;

    private readonly EntityEditorTransform _transform = transform;
    public ref readonly EntityEditorTransform Transform => ref _transform;
}
