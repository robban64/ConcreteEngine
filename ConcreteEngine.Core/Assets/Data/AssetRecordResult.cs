namespace ConcreteEngine.Core.Assets;

internal sealed record AssetRecordResult
{
    public AssetResourceManifest<TextureManifestRecord> Textures { get; init; }
    public AssetResourceManifest<ShaderManifestRecord> Shaders { get; init; }
    public AssetResourceManifest<MeshManifestRecord>? Meshes { get; init; }
    public AssetResourceManifest<CubeMapManifestRecord>? Cubemaps { get; init; }
    
}