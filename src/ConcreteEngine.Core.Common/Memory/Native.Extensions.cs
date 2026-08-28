using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public static class NativeExtensions
{
    public static NativeSpanWriter AsWriter(this NativeView<byte> viewPtr) => new(viewPtr);
    public static NativeAllocBuilder Allocator(this NativeView<byte> viewPtr) => new(viewPtr);

    extension<T>(NativeView<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PtrEnumerator<T, T2> Zip<T2>(NativeView<T2> view2) where T2 : unmanaged => new(it, view2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PtrEnumerator<T, T2, T3> Zip<T2, T3>(NativeView<T2> view2, NativeView<T3> view3)
            where T2 : unmanaged where T3 : unmanaged => new(it, view2, view3);
    }


    extension<T>(NativeArray<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);
    }
}