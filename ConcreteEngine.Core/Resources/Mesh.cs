using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Resources;

public sealed class Mesh : IGraphicAssetFile<MeshId>
{
    public string Name { get; init; }
    
    public string? Filename { get; init; }
    
    public MeshId ResourceId { get; init; }
    
    public MeshMeta Meta { get; init; }
    
    public AssetFileType AssetType => AssetFileType.Mesh;

}