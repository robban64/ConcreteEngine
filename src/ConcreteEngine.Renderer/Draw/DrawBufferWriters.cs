using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Renderer.Draw;

public readonly ref struct DrawCommandUploader
{
    private readonly Span<DrawObjectUniform> _transformSpan;
    private readonly Span<DrawCommand> _commandSpan;
    private readonly Span<DrawCommandMeta> _metaSpan;
    private readonly Span<DrawCommandRef> _indexBuffer;

    private readonly ref int _idx;

    internal DrawCommandUploader(
        int length,
        ref int idx,
        Span<DrawObjectUniform> transformBuffer,
        Span<DrawCommand> commandBuffer,
        Span<DrawCommandMeta> metaBuffer,
        Span<DrawCommandRef> indexBuffer)
    {
        var len = length + 1;
        if ((uint)len > commandBuffer.Length || commandBuffer.Length != transformBuffer.Length ||
            commandBuffer.Length != indexBuffer.Length || commandBuffer.Length != metaBuffer.Length)
        {
            throw new IndexOutOfRangeException();
        }

        _idx = ref idx;
        _commandSpan = commandBuffer.Slice(0, len);
        _metaSpan = metaBuffer.Slice(0, len);
        _indexBuffer = indexBuffer.Slice(0, len);
        _transformSpan = transformBuffer.Slice(0, len);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<DrawObjectUniform> SubmitDraw(in DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = _idx++;
        _commandSpan[idx] = cmd;
        _metaSpan[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);
        return new ValuePtr<DrawObjectUniform>(ref _transformSpan[idx]);
    }
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