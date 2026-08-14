using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public static class NativeExtensions
{
    public static NativeSpanWriter Writer(this NativeView<byte> viewPtr) => new(viewPtr);
    public static NativeAllocBuilder Allocator(this NativeView<byte> viewPtr) => new(viewPtr);

    extension<T>(NativeView<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);
    }


    extension<T>(NativeArray<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);
    }
}