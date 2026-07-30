using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeExtensions
{
    public static NativeSpanWriter Writer(this NativeView<byte> viewPtr) => new(viewPtr);

    extension<T>(NativeView<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RangeU16 AsRange16() => new(it.Offset, it.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Range32 AsRange32() => new(it.Offset, it.Length);
    }


    extension<T>(NativeArray<T> it) where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(RangeU16 range) => it.Slice(range.Offset16, range.Length16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<T> Slice(Range32 range) => it.Slice(range.Offset, range.Length);

    }
}