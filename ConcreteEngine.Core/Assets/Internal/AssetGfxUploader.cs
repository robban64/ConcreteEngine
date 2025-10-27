#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Assets.Internal;

internal sealed class AssetGfxUploader
{
    private readonly GfxMeshes _meshes;
    private readonly GfxTextures _textures;
    private readonly GfxShaders _shaders;

    internal AssetGfxUploader(GfxContext gfx)
    {
        _meshes = gfx.Meshes;
        _textures = gfx.Textures;
        _shaders = gfx.Shaders;
    }

    public MeshCreationInfo UploadMesh(MeshUploadPayload payload)
    {
        var vSpan = payload.Vertices;
        var iSpan = payload.Indices;

        var builder = _meshes.StartUploadBuilder(payload.Properties);
        builder.UploadVertices(vSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.UploadIndices(iSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.SetAttributeRange(payload.Attributes);
        var meshId = builder.Finish();
        return new MeshCreationInfo(meshId, payload.Indices.Length);
    }

    public void UploadTexture(in TexturePayload payload, out TextureCreationInfo info)
    {
        var desc = payload.TextureDesc;
        var textureId = _textures.BuildTexture(in desc, payload.TextureProps, payload.Data);
        info = new TextureCreationInfo(textureId, desc.Width, desc.Height, desc.Format);
    }

    public void UploadCubeMap(in CubeMapPayload payload, out CubeMapCreationInfo info)
    {
        var desc = payload.TextureDesc;
        var textureId = _textures.BuildCubeMap(in desc, payload.TextureProps, payload.FaceData);
        info = new CubeMapCreationInfo(textureId, desc.Width, desc.Format);
    }

    public void UploadShader(in ShaderPayload data, out ShaderCreationInfo info)
    {
        var shaderId = _shaders.CreateShader(data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }

    public void RecreateShader(ShaderId shaderId, in ShaderPayload data, out ShaderCreationInfo info)
    {
        _shaders.RecreateShader(shaderId, data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }
}