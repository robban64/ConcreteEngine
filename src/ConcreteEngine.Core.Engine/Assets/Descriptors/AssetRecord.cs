using System.Text.Json.Serialization;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

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
    public const string VertexFileKey = "Vertex";
    public const string FragmentFileKey = "Fragment";

    public string VertexShader { get; set; }
    public string FragmentShader { get; set; }

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Shader;

    [JsonIgnore]
    public override int FileCount => 2;

    public override string GetFile(int fileIndex) => fileIndex == 0 ? VertexShader : FragmentShader;
}

internal sealed class TextureRecord : AssetRecord
{
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

    public static TextureRecord Create(string filename, string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(filename);
        var isNormal = name.Contains("normal", StringComparison.OrdinalIgnoreCase);
        return new TextureRecord
        {
            Id = Guid.NewGuid(),
            Name = name,
            PixelFormat = isNormal ? TexturePixelFormat.Rgba : TexturePixelFormat.SrgbAlpha,
            Preset = isNormal ? TexturePreset.LinearMipmapRepeat : TexturePreset.LinearClamp,
            LoadMode = AssetLoadingMode.MemoryOnly,
            TextureFiles = [relativePath]
        };
    }
}

internal sealed class ModelRecord : AssetRecord
{
    public required string ModelFile { get; init; }
    public int SubMeshCount { get; init; }
    public bool HasAnimation { get; init; }

    [JsonIgnore]
    public override AssetKind Kind => AssetKind.Model;

    [JsonIgnore]
    public override int FileCount => 1;

    public override string GetFile(int fileIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fileIndex, 1);
        return ModelFile;
    }

    public static ModelRecord Create(string filename, string relativePath)
    {
        return new ModelRecord
        {
            Id = Guid.NewGuid(),
            Name = Path.GetFileNameWithoutExtension(filename),
            LoadMode = AssetLoadingMode.Processed,
            ModelFile = relativePath
        };
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