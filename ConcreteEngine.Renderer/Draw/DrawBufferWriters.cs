#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Renderer.Draw;

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

/*
public ref struct AnimationUniformWriter(Span<DrawAnimationUniform> data)
{
    public Span<DrawAnimationUniform> Data = data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillIdentity(int index, Range32 range) =>
        GetMatrices(index).Slice(range.Offset, range.Length).Fill(Matrix4x4.Identity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<Matrix4x4> GetMatrices(int index)
    {
        unsafe
        {
            ref float start = ref Unsafe.AsRef(ref Data[index].Weights[0]);
            var floatSpan = MemoryMarshal.CreateSpan(ref start, DrawAnimationUniform.TotalComponents);
            return MemoryMarshal.Cast<float, Matrix4x4>(floatSpan);
        }
    }

    public ref Matrix4x4 this[int index, int matrixIdx]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetMatrices(index)[matrixIdx];
    }
}*/