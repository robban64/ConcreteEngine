using System.Text.Json.Serialization;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "assetKind")]
[JsonDerivedType(typeof(ShaderRecord), typeDiscriminator: nameof(AssetKind.Shader))]
[JsonDerivedType(typeof(TextureRecord), typeDiscriminator: nameof(AssetKind.Texture))]
[JsonDerivedType(typeof(ModelRecord), typeDiscriminator: nameof(AssetKind.Model))]
[JsonDerivedType(typeof(MaterialRecord), typeDiscriminator: nameof(AssetKind.Material))]
public abstract class AssetRecord
{
    [JsonPropertyOrder(-10)]
    public AssetLoadingMode LoadMode { get; init; } = AssetLoadingMode.Processed;

    [JsonPropertyOrder(-9)]
    public required Guid Id { get; init; }

    [JsonPropertyOrder(-8)]
    public required string Name { get; init; }

    [JsonIgnore]
    public abstract AssetKind Kind { get; }

    [JsonIgnore]
    public abstract int FileCount { get; }

    public abstract string GetFile(int fileIndex);
}

internal sealed class ShaderRecord : AssetRecord
{
    public required string VertexShader { get; init; }
    public required string FragmentShader { get; init; }

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Shader;

    [JsonIgnore]
    public override int FileCount => 2;

    public override string GetFile(int fileIndex) => fileIndex == 0 ? VertexShader : FragmentShader;
}

internal sealed class TextureRecord : AssetRecord
{
    // TODO
    public float LodBias { get; init; }
    public bool InMemory { get; init; }

    public TexturePreset Preset { get; init; } = TexturePreset.LinearClamp;
    public TextureKind TextureKind { get; init; } = TextureKind.Texture2D;
    public TexturePixelFormat PixelFormat { get; init; } = TexturePixelFormat.SrgbAlpha;
    public AnisotropyLevel Anisotropy { get; init; } = AnisotropyLevel.Off;

    public required string[] TextureFiles { get; init; }

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Texture;

    [JsonIgnore]
    public override int FileCount => TextureFiles.Length;

    public override string GetFile(int fileIndex) => TextureFiles[fileIndex];
}

internal sealed class ModelRecord : AssetRecord
{
    public required string ModelFile { get; init; }

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Model;

    [JsonIgnore]
    public override int FileCount => 1;

    public override string GetFile(int fileIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fileIndex, 1);
        return ModelFile;
    }
}

internal sealed class MaterialRecord : AssetRecord
{
    public MaterialProfileId Profile { get; init; } = MaterialProfileId.Opaque;
    public string?[] ProfileSlots { get; init; } = [];
    public MaterialStateRecord? Parameters { get; init; }

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Material;

    [JsonIgnore]
    public override int FileCount => 0;

    public override string GetFile(int fileIndex) => throw new InvalidOperationException("Materials have no files");
}