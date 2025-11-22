#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Assets.Internal;

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

    public void UploadMesh<T>(MeshUploadData<T> data) where T : unmanaged
    {
        var vSpan = data.Vertices;
        var iSpan = data.Indices;

        var properties =  MeshDrawProperties.MakeElemental(drawCount: iSpan.Length);
        
        var builder = _meshes.StartUploadBuilder(in properties);
        builder.UploadVertices(vSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.UploadIndices(iSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        
        Span<VertexAttribute> attribs = stackalloc VertexAttribute[6];
        if (typeof(T) == typeof(Vertex3DSkinned))
        {
            FillAttributes(attribs, isAnimated: true);
            builder.SetAttributeSpan(attribs);
        }
        else
        {
            FillAttributes(attribs, isAnimated: false);
            builder.SetAttributeSpan(attribs.Slice(0,4));
        }
        
        var meshId = _meshes.FinishUploadBuilder(out var meta);
        data.Result = new MeshCreationInfo(meshId, meta.DrawCount);
    }
    
    public void UploadTexture(ReadOnlySpan<byte> data, in TextureUploadMeta meta, out TextureCreationInfo info)
    {
        var desc = meta.TextureDesc;
        var textureId = _textures.BuildTexture(in desc, meta.TextureProps, data);
        info = new TextureCreationInfo(textureId, desc.Width, desc.Height, desc.Format);
    }

    public void UploadCubeMap(ReadOnlyMemory<byte>[] data, in TextureUploadMeta meta, out CubeMapCreationInfo info)
    {
        var desc = meta.TextureDesc;
        var textureId = _textures.BuildCubeMap(in desc, meta.TextureProps, data);
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

    
    private static void FillAttributes(Span<VertexAttribute> attribs, bool isAnimated)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(attribs.Length, 6, nameof(attribs));
        var attribBuilder = new VertexAttributeMaker();

        attribs[0] = attribBuilder.Make<Vector3>(0);
        attribs[1] = attribBuilder.Make<Vector2>(1);
        attribs[2] = attribBuilder.Make<Vector3>(2);
        attribs[3] = attribBuilder.Make<Vector3>(3);
        
        if (isAnimated)
        {
            attribs[4] = attribBuilder.Make<Int4>(4);
            attribs[5] = attribBuilder.Make<Vector4>(5);
        }
    }
}