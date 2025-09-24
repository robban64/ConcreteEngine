using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets.Factories;

internal interface IAssetAssembler
{
    AssetKind Kind { get; }
    void Assemble(IAssetFinalEntry entry, AssetSystem system);
}

internal abstract class AssetAssembler<TRecord, TDesc, TId, TAsset> : IAssetAssembler
    where TRecord : class, IAssetManifestRecord
    where TDesc : struct
    where TId : unmanaged, IResourceId
    where TAsset : class, IGraphicAssetFile
{
    public AssetKind Kind => TRecord.Kind;
    protected abstract TAsset Build(TRecord record, in TDesc desc, TId id);

    public void Assemble(IAssetFinalEntry entry, AssetSystem system)
    {
        if (entry is not AssetFinalEntry<TRecord, TDesc, TId> e)
            throw new InvalidOperationException($"Wrong entry type for {Kind}");

        var asset = Build(e.Record, e.Descriptor, e.ResourceId);
        system.AddResource(asset);
    }
}

internal sealed class ShaderAssembler
    : AssetAssembler<ShaderManifestRecord, ShaderCreationInfo, ShaderId, Shader>
{
    protected override Shader Build(ShaderManifestRecord record, in ShaderCreationInfo info, ShaderId id)
    {
        return new Shader
        {
            Name = record.Name,
            FragShaderFilename = record.FragmentFilename,
            VertShaderFilename = record.VertexFilename,
            ResourceId = id,
            Samplers = info.Samplers
        };
    }
}


internal sealed class TextureAssembler
    : AssetAssembler<TextureManifestRecord, TextureCreationInfo, TextureId, Texture2D>
{
    protected override Texture2D Build(TextureManifestRecord record, in TextureCreationInfo info, TextureId id)
    {
        var texture = new Texture2D
        {
            Name = record.Name,
            Path = record.Filename,
            ResourceId = id,
            Width = info.Width,
            Height = info.Height,
            PixelFormat = info.PixelFormat,
            Preset = record.Preset,
            Anisotropy = record.Anisotropy,
        };
        if (info.Data is { } tData)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(tData.Length, 0);
            texture.SetPixelData(tData);
        }

        return texture;
    }
}

internal sealed class CubeMapAssembler
    : AssetAssembler<CubeMapManifestRecord, CubeMapCreationInfo, TextureId, CubeMap>
{
    protected override CubeMap Build(CubeMapManifestRecord record, in CubeMapCreationInfo info, TextureId id)
    {
        return new CubeMap
        {
            Name = record.Name,
            ResourceId = id,
            Textures = record.Textures,
            Width = record.Width,
            Height = record.Height,
            PixelFormat = info.PixelFormat
        };
    }
}


internal sealed class MeshAssembler
    : AssetAssembler<MeshManifestRecord, MeshCreationInfo, MeshId, Mesh>
{
    protected override Mesh Build(MeshManifestRecord record, in MeshCreationInfo info, MeshId id)
    {
        return new Mesh
        {
            Name = record.Name,
            Filename = record.Filename,
            DrawCount = info.DrawCount,
            ResourceId = id
        };
    }
}

