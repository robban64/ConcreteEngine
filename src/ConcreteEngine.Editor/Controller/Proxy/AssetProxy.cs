using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.Controller.Proxy;

internal sealed class AssetProxy
{
    public readonly AssetId Id;
    public readonly Guid GId;
    public readonly AssetKind Kind;
}

public sealed class EditorTextureInspector
{
    public readonly FloatInputValueField<Float1Value> LodLevel;
    public readonly ComboField Preset;
    public readonly ComboField Anisotropy;
    public readonly ComboField Usage;
    public readonly ComboField PixelFormat;
}

public sealed class EditorModelInspector
{
}