using System.Numerics;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.Internal;

internal sealed class AssetGfxUploader(GfxContext gfx)
{

    public void UploadMesh<T>(MeshUploadData<T> data) where T : unmanaged
    {
        var vSpan = data.Vertices;
        var iSpan = data.Indices;

        var properties = MeshDrawProperties.MakeElemental(drawCount: iSpan.Length);

        var builder = gfx.Meshes.StartUploadBuilder(in properties);
        builder.UploadVertices(vSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.UploadIndices(iSpan, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);

        Span<VertexAttribute> attrib = stackalloc VertexAttribute[6];
        if (typeof(T) == typeof(VertexSkinned))
        {
            FillAttributes(attrib, isAnimated: true);
            builder.SetAttributeSpan(attrib);
        }
        else
        {
            FillAttributes(attrib, isAnimated: false);
            builder.SetAttributeSpan(attrib.Slice(0, 4));
        }

        var meshId = gfx.Meshes.FinishUploadBuilder(out var meta);
        data.Result = new MeshCreationInfo(meshId, meta.DrawCount);
    }

    public void UploadTexture(ReadOnlySpan<byte> data, in TextureUploadMeta meta, out TextureCreationInfo info)
    {
        var desc = meta.TextureDesc;
        var textureId = gfx.Textures.BuildTexture(in desc, meta.TextureProps, data);
        info = new TextureCreationInfo(textureId, desc.Width, desc.Height);
    }

    public void UploadCubeMap(ReadOnlyMemory<byte>[] data, in TextureUploadMeta meta, out TextureCreationInfo info)
    {
        var desc = meta.TextureDesc;
        var textureId = gfx.Textures.BuildCubeMap(in desc, meta.TextureProps, data);
        info = new TextureCreationInfo(textureId, desc.Width, desc.Height);
    }

    public void UploadShader(in ShaderPayload data, out ShaderCreationInfo info)
    {
        var shaderId = gfx.Shaders.CreateShader(data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }

    public void RecreateShader(ShaderId shaderId, in ShaderPayload data, out ShaderCreationInfo info)
    {
        gfx.Shaders.RecreateShader(shaderId, data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }


    private static void FillAttributes(Span<VertexAttribute> attrib, bool isAnimated)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(attrib.Length, 6, nameof(attrib));
        var attribBuilder = new VertexAttributeMaker();

        attrib[0] = attribBuilder.Make<Vector3>(0);
        attrib[1] = attribBuilder.Make<Vector2>(1);
        attrib[2] = attribBuilder.Make<Vector3>(2);
        attrib[3] = attribBuilder.Make<Vector3>(3);

        if (isAnimated)
        {
            attrib[4] = attribBuilder.Make<Int4>(4, vertexFormat: VertexFormat.Integer);
            attrib[5] = attribBuilder.Make<Vector4>(5);
        }
    }
}