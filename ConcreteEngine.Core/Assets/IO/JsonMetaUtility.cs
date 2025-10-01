#region

using System.Text.Json.Serialization;
using ConcreteEngine.Core.Assets.Manifest;

#endregion

namespace ConcreteEngine.Core.Assets.IO;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AssetResourceManifest<TextureManifestRecord>))]
[JsonSerializable(typeof(AssetResourceManifest<ShaderManifestRecord>))]
[JsonSerializable(typeof(AssetResourceManifest<CubeMapManifestRecord>))]
[JsonSerializable(typeof(AssetResourceManifest<MeshManifestRecord>))]
[JsonSerializable(typeof(AssetResourceManifest<MaterialManifestRecord>))]
[JsonSerializable(typeof(AssetResourceManifest<MaterialManifestRecord>))]
internal partial class ManifestJsonContext : JsonSerializerContext;

/*
internal class ManifestTypeInfo
{
    public static JsonTypeInfo<AssetResourceManifest<T>> Get<T>() where T : IAssetManifestRecord =>
        (JsonTypeInfo<AssetResourceManifest<T>>)GetUntyped(typeof(T));

    private static JsonTypeInfo GetUntyped(Type t)
    {
        if (t == typeof(TextureManifestRecord))
            return ManifestJsonContext.Default.AssetResourceManifestTextureManifestRecord;
        if (t == typeof(ShaderManifestRecord))
            return ManifestJsonContext.Default.AssetResourceManifestShaderManifestRecord;
        if (t == typeof(CubeMapManifestRecord))
            return ManifestJsonContext.Default.AssetResourceManifestCubeMapManifestRecord;
        if (t == typeof(MeshManifestRecord))
            return ManifestJsonContext.Default.AssetResourceManifestMeshManifestRecord;
        if (t == typeof(MaterialManifestRecord))
            return ManifestJsonContext.Default.AssetResourceManifestMaterialManifestRecord;

        throw new NotSupportedException($"No JsonTypeInfo registered for {t.Name}.");
    }
}*/