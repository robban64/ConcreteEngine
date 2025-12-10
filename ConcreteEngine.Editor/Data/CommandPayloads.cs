#region

using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.Data;

public interface ICommandPayload
{
    EditorRequestAction RequestAction { get; }
}

public sealed record EditorShadowCommand(int Size, bool Enabled, EditorRequestAction RequestAction) : ICommandPayload;

public sealed record EditorShaderCommand(string Name, EditorRequestAction RequestAction) : ICommandPayload;