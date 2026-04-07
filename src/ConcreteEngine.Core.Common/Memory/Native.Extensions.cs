using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeViewPtr<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);

    extension<T>(NativeViewPtr<T> it) where T : unmanaged
    {
        public static NativeViewPtr<T> MakeNull() => new(null, 0, 0);

        public NativeViewPtr<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);
        public NativeViewPtr<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeViewPtr<T> Slice(int offset, int length)
        {
            Debug.Assert((uint)offset + (uint)length <= (uint)it.Length);
            return new NativeViewPtr<T>(it.Ptr + offset, it.Offset + offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeViewPtr<T> SliceFrom(int offset)
        {
            Debug.Assert((uint)offset <= (uint)it.Length);
            return new NativeViewPtr<T>(it.Ptr + offset, offset, it.Length - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int offset = 0)
        {
            Debug.Assert((uint)offset <= (uint)it.Length);
            return new Span<T>(it.Ptr + offset, it.Length - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int offset, int length)
        {
            Debug.Assert((uint)offset + (uint)length <= (uint)it.Length);
            return new Span<T>(it.Ptr + offset, length);
        }
    }


    extension<T>(NativeArray<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeViewPtr<T> Slice(int offset, int length = 0)
        {
            Debug.Assert((uint)offset + (uint)length <= (uint)it.Length);
            return new NativeViewPtr<T>(it.Ptr + offset, offset, length > 0 ? length : it.Length - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int offset, int length)
        {
            Debug.Assert((uint)offset + (uint)length <= (uint)it.Length);
            return new Span<T>(it.Ptr + offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan(int offset, int length)
        {
            Debug.Assert((uint)offset + (uint)length <= (uint)it.Length);
            return new ReadOnlySpan<T>(it.Ptr + offset, length);
        }
    }
}