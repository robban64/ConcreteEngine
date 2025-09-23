using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Loaders;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetGfxUploader
{
    private GfxContext _gfx;

    internal AssetGfxUploader(GfxContext gfx)
    {
        _gfx = gfx;
    }
    
    public MeshId UploadMesh(MeshManifestRecord record, in MeshLoaderResult payload)
    {
        ReadOnlySpan<Vertex3D> vSpan = CollectionsMarshal.AsSpan(payload.Vertices);
        ReadOnlySpan<uint> iSpan = CollectionsMarshal.AsSpan(payload.Indices);

        var builder = _gfx.Meshes.StartUploadBuilder(payload.Properties);
        builder.UploadVertices(vSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.UploadIndices(iSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.SetAttributeRange(payload.Attributes);
        var meshId = builder.Finish();

        return new Mesh
        {
            Name = record.Name,
            Filename = record.Filename,
            DrawCount = payload.Properties.DrawCount,
            ResourceId = meshId
        };
    }

    public TextureId UploadTexture(TextureManifestRecord record, in TexturePayload payload)
    {
        var desc = payload.Descriptor;
        var id = _gfx.Textures.CreateTexture2D(payload.Data.Span, in desc);

        //var data = record.InMemory ? _dataCache[record.Name] : null;
        return new Texture2D
        {
            Name = record.Name,
            Path = record.Filename,
            ResourceId = id,
            Width = payload.Descriptor.Width,
            Height = payload.Descriptor.Height,
            PixelFormat = payload.Descriptor.Format,
            Preset = record.Preset,
            Anisotropy = record.Anisotropy
        };
    }
    
    public TextureId UploadCubeMap(CubeMapManifestRecord record, in CubeMapPayload payload)
    {
        var desc = payload.Descriptor;
        var textureId = _gfx.Textures.CreateCubeMap(in desc);
        _gfx.Textures.UploadCubeMapFace(textureId, payload.Data.Span, desc.Width, desc.Height, 0);
        for (int i = 1; i < 6; i++)
        {
            var face = loader.LoadFaceData(record!, i);
            _gfx.Textures.UploadCubeMapFace(textureId, face.Data, face.Descriptor.Width, face.Descriptor.Height, i);
        }

        return new CubeMap
        {
            Name = record!.Name,
            ResourceId = textureId,
            Textures = record.Textures,
            Width = record.Width,
            Height = record.Height,
            PixelFormat = payload.Descriptor.Format
        };
    }

    public ShaderId UploadShader(ShaderManifestRecord record, in ShaderPayload data)
    {
        var id = _gfx.Shaders.CreateShader(data.Vs, data.Fs, out var samplers);

        return new Shader
        {
            Name = record.Name,
            FragShaderFilename = record.FragmentFilename,
            VertShaderFilename = record.VertexFilename,
            ResourceId = id,
            Samplers = samplers
        };
    }
}