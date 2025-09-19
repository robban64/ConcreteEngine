using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Resources;

public sealed class Mesh : IGraphicAssetFile<MeshId>
{
    public string Name { get; init; }

    public string? Filename { get; init; }

    public bool IsStatic { get; init; }

    public uint DrawCount { get; init; }

    public AssetFileType AssetType => AssetFileType.Mesh;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;
    public MeshId ResourceId { get; init; }
}