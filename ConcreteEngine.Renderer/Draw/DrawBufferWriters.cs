using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
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
    public int SubmitDraw(in DrawCommand cmd, DrawCommandMeta meta) => _cmdBuffer.Submit(in cmd, meta);

    public int SubmitDrawAndTransform(DrawCommand cmd, DrawCommandMeta meta, in Matrix4x4 model, in Matrix3X4 normal) =>
        _cmdBuffer.SubmitDraw(cmd, meta, in model, in normal);

    public int SubmitDrawIdentity(DrawCommand cmd, DrawCommandMeta meta) => _cmdBuffer.SubmitDrawIdentity(cmd, meta);

}

public readonly ref struct SkinningBufferUploader
{
    private readonly Matrix4x4[] _boneTransforms;
    private readonly DrawCommandBuffer _cmdBuffer;

    internal SkinningBufferUploader(
        DrawCommandBuffer cmdBuffer,
        Matrix4x4[] boneTransforms)
    {
        _cmdBuffer = cmdBuffer;
        _boneTransforms = boneTransforms;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<Matrix4x4> GetWriter()
    {
        var index = _cmdBuffer.IncrementSkinningIndex();
        return _boneTransforms.AsSpan(index * RenderLimits.BoneCapacity, RenderLimits.BoneCapacity);
    }
}
/*
public ref struct AnimationUniformWriter(ref DrawAnimationUniform data)
{
    public ref DrawAnimationUniform Data = ref data;
    public ref int Slot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillIdentity(Range32 range) => Matrices.Slice(range.Offset, range.Length).Fill(Matrix4x4.Identity);

    public Span<Matrix4x4> Matrices
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe
            {
                ref float start = ref Unsafe.AsRef(ref Data.Weights[0]);
                var floatSpan = MemoryMarshal.CreateSpan(ref start, DrawAnimationUniform.TotalComponents);
                return MemoryMarshal.Cast<float, Matrix4x4>(floatSpan);
            }
        }
    }

    public ref Matrix4x4 this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Matrices[index];
    }
}
*/