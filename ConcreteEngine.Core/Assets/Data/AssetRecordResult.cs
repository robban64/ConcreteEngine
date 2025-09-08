namespace ConcreteEngine.Core.Assets;

internal sealed record AssetRecordResult
{
    public IReadOnlyList<TextureManifestRecord> Textures { get; init; }
    public IReadOnlyList<CubeMapManifestRecord> Cubemaps { get; init; }
    public IReadOnlyList<ShaderManifestRecord> Shaders { get; init; }
    public IReadOnlyList<MeshManifestRecord> Meshes { get; init; }
    
}