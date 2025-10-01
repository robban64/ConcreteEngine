#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Loaders;
using ConcreteEngine.Core.Assets.Manifest;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets;

internal sealed class AssetGfxUploader
{
    private GfxContext _gfx;

    internal AssetGfxUploader(GfxContext gfx)
    {
        _gfx = gfx;
    }

    public MeshId UploadMesh(MeshManifestRecord record, in MeshResultPayload payload, out MeshCreationInfo info)
    {
        ReadOnlySpan<Vertex3D> vSpan = CollectionsMarshal.AsSpan(payload.Vertices);
        ReadOnlySpan<uint> iSpan = CollectionsMarshal.AsSpan(payload.Indices);

        var builder = _gfx.Meshes.StartUploadBuilder(payload.Properties);
        builder.UploadVertices(vSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.UploadIndices(iSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.SetAttributeRange(payload.Attributes);
        var meshId = builder.Finish();
        info = new MeshCreationInfo();
        return meshId;
    }

    public TextureId UploadTexture(TextureManifestRecord record, in TexturePayload payload,
        out TextureCreationInfo info)
    {
        var desc = payload.TextureDesc;
        var inMemoryData = record.InMemory ? payload.Data : null;
        info = new TextureCreationInfo(desc.Width, desc.Height, desc.Format, inMemoryData);
        var textureId = _gfx.Textures.BuildTexture(in desc, payload.TextureProps, payload.Data);
        return textureId;
    }

    public TextureId UploadCubeMap(CubeMapManifestRecord record, in CubeMapPayload payload,
        out CubeMapCreationInfo info)
    {
        var desc = payload.TextureDesc;
        var textureId = _gfx.Textures.BuildCubeMap(in desc, payload.TextureProps, payload.FaceData);
        info = new CubeMapCreationInfo(desc.Width, desc.Height, desc.Format);
        return textureId;
    }

    public ShaderId UploadShader(ShaderManifestRecord record, in ShaderPayload data, out ShaderCreationInfo info)
    {
        var shaderId = _gfx.Shaders.CreateShader(data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(samplers);
        return shaderId;
    }
}