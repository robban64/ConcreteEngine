#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Editor.Data;

public sealed record EditorShadowPayload(int Size, bool Enabled, EditorRequestAction RequestAction);

public sealed record EditorShaderPayload(string Name, EditorRequestAction RequestAction);
