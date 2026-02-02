using System.Numerics;
using System.Runtime.CompilerServices;
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
    public MeshScratchpad GetMeshScratchpad() => gfx.MeshScratchpad;
    
    private const BufferUsage BuffUsage = BufferUsage.StaticDraw;
    private const BufferStorage BuffStore = BufferStorage.Static;
    private const BufferAccess BuffAccess = BufferAccess.None;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public MeshId UploadMesh(MeshDataSpan data)
    {
        var properties = MeshDrawProperties.MakeElemental(drawCount: data.Indices.Length);

        var builder = gfx.Meshes.StartUploadBuilder(in properties);
        builder.UploadVertices(data.Vertices, BuffUsage, BuffStore, BuffAccess);
        builder.UploadIndices(data.Indices, BuffUsage, BuffStore, BuffAccess);

        Span<VertexAttribute> attrib = stackalloc VertexAttribute[4];
        FillAttributes(attrib);
        builder.SetAttributeSpan(attrib);
        return gfx.Meshes.FinishUploadBuilder(out _);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public MeshId UploadAnimatedMesh(MeshSkinnedDataSpan data)
    {
        var properties = MeshDrawProperties.MakeElemental(drawCount: data.Indices.Length);

        var builder = gfx.Meshes.StartUploadBuilder(in properties);
        builder.UploadVertices(data.Vertices, BuffUsage, BuffStore, BuffAccess);
        builder.UploadVertices(data.Skinned, BuffUsage, BuffStore, BuffAccess);
        builder.UploadIndices(data.Indices, BuffUsage, BuffStore, BuffAccess);

        Span<VertexAttribute> attrib = stackalloc VertexAttribute[6];
        FillAnimatedAttributes(attrib);
        builder.SetAttributeSpan(attrib);
        return gfx.Meshes.FinishUploadBuilder(out _);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UploadTexture(ReadOnlySpan<byte> data, in TextureUploadMeta meta, out TextureCreationInfo info)
    {
        var desc = meta.TextureDesc;
        var textureId = gfx.Textures.BuildTexture(in desc, meta.TextureProps, data);
        info = new TextureCreationInfo(textureId, desc.Width, desc.Height);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UploadCubeMap(ReadOnlyMemory<byte>[] data, in TextureUploadMeta meta, out TextureCreationInfo info)
    {
        var desc = meta.TextureDesc;
        var textureId = gfx.Textures.BuildCubeMap(in desc, meta.TextureProps, data);
        info = new TextureCreationInfo(textureId, desc.Width, desc.Height);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UploadShader(in ShaderPayload data, out ShaderCreationInfo info)
    {
        var shaderId = gfx.Shaders.CreateShader(data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RecreateShader(ShaderId shaderId, in ShaderPayload data, out ShaderCreationInfo info)
    {
        gfx.Shaders.RecreateShader(shaderId, data.Vs, data.Fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillAttributes(Span<VertexAttribute> attrib)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(attrib.Length, 4, nameof(attrib));
        var attribBuilder = new VertexAttributeMaker();
        attrib[0] = attribBuilder.Make<Vector3>(0);
        attrib[1] = attribBuilder.Make<Vector2>(1);
        attrib[2] = attribBuilder.Make<Vector3>(2);
        attrib[3] = attribBuilder.Make<Vector3>(3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillAnimatedAttributes(Span<VertexAttribute> attrib)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(attrib.Length, 6, nameof(attrib));
        var attribBuilder = new VertexAttributeMaker();
        attrib[0] = attribBuilder.Make<Vector3>(0);
        attrib[1] = attribBuilder.Make<Vector2>(1);
        attrib[2] = attribBuilder.Make<Vector3>(2);
        attrib[3] = attribBuilder.Make<Vector3>(3);

        attribBuilder.ResetOffset();
        attrib[4] = attribBuilder.Make<Int4>(4, binding:1, vertexFormat: VertexFormat.Integer);
        attrib[5] = attribBuilder.Make<Vector4>(5, binding:1);
    }
}