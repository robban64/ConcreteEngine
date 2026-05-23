using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Types;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetGfxUploader(GfxContext gfx)
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public MeshId UploadMesh(NativeView<Vertex3D> vertices, NativeView<byte> indices, bool is16Bit)
    {
        var drawSize = is16Bit ? DrawElementSize.UnsignedShort :  DrawElementSize.UnsignedInt;
        var properties = MeshDrawProperties.MakeElemental(drawCount: indices.Length, size: drawSize);
        var iboArgs = CreateIboArgs.MakeDefault();

        var meshId = gfx.Meshes.CreateEmptyMesh(in properties, 1, VertexAttributes.GetVertex3DAttributes());
        gfx.Meshes.CreateAttachVertexBuffer(meshId, vertices.AsReadOnlySpan(), CreateVboArgs.MakeDefault(0));
        if(is16Bit)
            gfx.Meshes.CreateAttachIndexBuffer(meshId, indices.Reinterpret<ushort>().AsReadOnlySpan(), iboArgs);
        else
            gfx.Meshes.CreateAttachIndexBuffer(meshId, indices.Reinterpret<uint>().AsReadOnlySpan(), iboArgs);
        return meshId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public MeshId UploadAnimatedMesh(NativeView<Vertex3D> vertices, NativeView<SkinningData> skinned,
        NativeView<byte> indices, bool is16Bit)
    {
        var drawSize = is16Bit ? DrawElementSize.UnsignedShort :  DrawElementSize.UnsignedInt;
        var properties = MeshDrawProperties.MakeElemental(drawCount: indices.Length, size: drawSize);
        var iboArgs = CreateIboArgs.MakeDefault();

        var meshId = gfx.Meshes.CreateEmptyMesh(in properties, 2, VertexAttributes.GetSkinnedAttributes());
        gfx.Meshes.CreateAttachVertexBuffer(meshId, vertices.AsReadOnlySpan(), CreateVboArgs.MakeDefault(0));
        gfx.Meshes.CreateAttachVertexBuffer(meshId, skinned.AsReadOnlySpan(), CreateVboArgs.MakeDefault(1));
        if(is16Bit)
            gfx.Meshes.CreateAttachIndexBuffer(meshId, indices.Reinterpret<ushort>().AsReadOnlySpan(), iboArgs);
        else
            gfx.Meshes.CreateAttachIndexBuffer(meshId, indices.Reinterpret<uint>().AsReadOnlySpan(), iboArgs);
        return meshId;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UploadTexture(ReadOnlySpan<byte> data, in TextureUploadMeta meta, out TextureCreationInfo info)
    {
        var textureId = gfx.Textures.CreateTexture2D(meta.Size, meta.TextureProps, data);
        info = new TextureCreationInfo(textureId, meta.Size);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public unsafe void UploadCubeMap(NativeView<byte>* data, in TextureUploadMeta meta, out TextureCreationInfo info)
    {
        var textureId = gfx.Textures.CreateCubeMap(meta.Size, meta.TextureProps, data);
        info = new TextureCreationInfo(textureId, meta.Size);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UploadShader(NativeView<byte> vs, NativeView<byte> fs, out ShaderCreationInfo info)
    {
        var shaderId = gfx.Shaders.CreateShader(vs, fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RecreateShader(ShaderId shaderId, NativeView<byte> vs, NativeView<byte> fs,
        out ShaderCreationInfo info)
    {
        gfx.Shaders.RecreateShader(shaderId, vs, fs, out var samplers);
        info = new ShaderCreationInfo(shaderId, samplers);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillAttributes(Span<VertexAttributeDef> attrib)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(attrib.Length, 4, nameof(attrib));
        var attribBuilder = new VertexAttributeMaker();
        attrib[0] = attribBuilder.Make<Vector3>(0);
        attrib[1] = attribBuilder.Make<Vector2>(1);
        attrib[2] = attribBuilder.Make<Vector3>(2);
        attrib[3] = attribBuilder.Make<Vector3>(3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillAnimatedAttributes(Span<VertexAttributeDef> attrib)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(attrib.Length, 6, nameof(attrib));
        var attribBuilder = new VertexAttributeMaker();
        attrib[0] = attribBuilder.Make<Vector3>(0);
        attrib[1] = attribBuilder.Make<Vector2>(1);
        attrib[2] = attribBuilder.Make<Vector3>(2);
        attrib[3] = attribBuilder.Make<Vector3>(3);

        attribBuilder.ResetOffset();
        attrib[4] = attribBuilder.Make<Int4>(4, binding: 1, vertexFormat: VertexFormat.Integer);
        attrib[5] = attribBuilder.Make<Vector4>(5, binding: 1);
    }
}