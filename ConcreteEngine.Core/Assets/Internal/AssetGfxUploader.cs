#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Assets.Internal;

internal sealed class AssetGfxUploader
{
    private GfxContext _gfx;

    internal AssetGfxUploader(GfxContext gfx)
    {
        _gfx = gfx;
    }

    public void UploadMesh(in MeshResultPayload payload, out MeshCreationInfo info)
    {
        ReadOnlySpan<Vertex3D> vSpan = CollectionsMarshal.AsSpan(payload.Vertices);
        ReadOnlySpan<uint> iSpan = CollectionsMarshal.AsSpan(payload.Indices);

        var builder = _gfx.Meshes.StartUploadBuilder(payload.Properties);
        builder.UploadVertices(vSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.UploadIndices(iSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.SetAttributeRange(payload.Attributes);
        var meshId = builder.Finish();
        info = new MeshCreationInfo(meshId, 0);
    }

    public void UploadTexture(in TexturePayload payload, out TextureCreationInfo info)
    {
        var desc = payload.TextureDesc;
        var textureId = _gfx.Textures.BuildTexture(in desc, payload.TextureProps, payload.Data);
        info = new TextureCreationInfo(textureId, desc.Width, desc.Height, desc.Format);
    }

    public void UploadCubeMap(in CubeMapPayload payload, out CubeMapCreationInfo info)
    {
        var desc = payload.TextureDesc;
        var textureId = _gfx.Textures.BuildCubeMap(in desc, payload.TextureProps, payload.FaceData);
        info = new CubeMapCreationInfo(textureId, desc.Width, desc.Format);
    }

    public void UploadShader(in ShaderPayload data, out ShaderCreationInfo info)
    {
        var shaderId = _gfx.Shaders.CreateShader(data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }
}