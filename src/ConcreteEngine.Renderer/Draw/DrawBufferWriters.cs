using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Renderer.Draw;

public readonly ref struct DrawCommandUploader
{
    private readonly Span<DrawObjectUniform> _transformBuffer;
    private readonly DrawCommandBuffer _cmdBuffer;

    internal DrawCommandUploader(
        DrawCommandBuffer cmdBuffer,
        DrawObjectUniform[] transformBuffer)
    {
        _cmdBuffer = cmdBuffer;
        _transformBuffer = transformBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawObjectUniform GetWriter() => ref _transformBuffer[_cmdBuffer.IncrementTransformIndex()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SubmitDraw(in DrawCommand cmd, in DrawCommandMeta meta) => _cmdBuffer.Submit(in cmd, meta);

    public int SubmitDrawAndTransform(DrawCommand cmd, DrawCommandMeta meta, in Matrix4x4 model, in Matrix3X4 normal) =>
        _cmdBuffer.SubmitDraw(cmd, meta, in model, in normal);

    public int SubmitDrawIdentity(DrawCommand cmd, DrawCommandMeta meta) => _cmdBuffer.SubmitDrawIdentity(cmd, meta);
}

public readonly ref struct SkinningBufferUploader
{
    private readonly Span<Matrix4x4> _boneTransforms;
    private readonly DrawCommandBuffer _cmdBuffer;

    internal SkinningBufferUploader(
        DrawCommandBuffer cmdBuffer,
        Matrix4x4[] boneTransforms)
    {
        _cmdBuffer = cmdBuffer;
        _boneTransforms = boneTransforms;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanSlice<Matrix4x4> GetWriter()
    {
        var index = _cmdBuffer.IncrementSkinningIndex();
        return new SpanSlice<Matrix4x4>(_boneTransforms, index * RenderLimits.BoneCapacity, RenderLimits.BoneCapacity);
    }
}