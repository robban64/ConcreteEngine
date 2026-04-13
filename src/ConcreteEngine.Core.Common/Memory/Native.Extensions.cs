using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeView<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);

    extension<T>(NativeView<T> it) where T : unmanaged
    {
        public static NativeView<T> MakeNull() => new(null, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);
        
        public RangeU16 AsRange16() => new (it.Offset, it.Length);
        public Range32 AsRange32() => new (it.Offset, it.Length);
    }


    extension<T>(NativeArray<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan(int offset, int length)
        {
            Debug.Assert((uint)offset + (uint)length <= (uint)it.Length);
            return new ReadOnlySpan<T>(it.Ptr + offset, length);
        }
    }
}