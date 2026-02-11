using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.Controller.Proxy;

internal sealed class AssetProxy
{
    public readonly AssetId Id;
    public readonly Guid GId;
    public readonly AssetKind Kind;
}

public sealed class AssetTextureProxy
{
    public readonly FloatInputValueField<Float1Value> LodLevel;
    public readonly ComboField Preset;
    public readonly ComboField Anisotropy;
    public readonly ComboField Usage;
    public readonly ComboField PixelFormat;
}

public sealed class AssetMeshProxy
{
    public Func<ModelInfo> GetModelInfo;
    public Func<int, string> GetMeshName;
    public Func<ReadOnlySpan<MeshInfo>> GetMeshes;
}