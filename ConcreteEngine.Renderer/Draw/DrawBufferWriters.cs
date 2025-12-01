#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Renderer.Draw;

public readonly ref struct DrawCommandUploader
{
    private readonly DrawObjectUniform[] _transformBuffer;
    private readonly int _idx;
    private readonly DrawCommandBuffer _cmdBuffer;

    internal DrawCommandUploader(
        int idx,
        DrawCommandBuffer cmdBuffer,
        DrawObjectUniform[] transformBuffer)
    {
        _idx = idx;
        _cmdBuffer = cmdBuffer;
        _transformBuffer = transformBuffer;
    }

    public int UploadDrawCommand(DrawCommand cmd, DrawCommandMeta meta)
    {
        return _cmdBuffer.Submit(cmd, meta);
    }
    
    public ref DrawObjectUniform WriteBuffer(int idx)
    {
        var index = _idx + idx;
        if ((uint)index >= _transformBuffer.Length) throw new IndexOutOfRangeException();
        return ref _transformBuffer[index];
    }
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

    public Span<Matrix4x4> WriteBoneSpan()
    {
        var index = _cmdBuffer.IncrementSkinningIndex();
        return _boneTransforms.AsSpan(index * RenderLimits.BoneCapacity, RenderLimits.BoneCapacity);
    }
}



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
