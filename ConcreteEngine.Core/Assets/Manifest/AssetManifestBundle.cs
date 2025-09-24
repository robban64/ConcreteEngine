namespace ConcreteEngine.Core.Assets.Manifest;

internal sealed record AssetManifestBundle
{
    public AssetResourceManifest<TextureManifestRecord> Textures { get; init; }
    public AssetResourceManifest<ShaderManifestRecord> Shaders { get; init; }
    public AssetResourceManifest<MeshManifestRecord>? Meshes { get; init; }
    public AssetResourceManifest<CubeMapManifestRecord>? Cubemaps { get; init; }
}