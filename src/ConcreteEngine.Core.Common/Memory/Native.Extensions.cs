using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeViewPtr<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);

    extension<T>(NativeViewPtr<T> it) where T : unmanaged
    {
        public NativeViewPtr<T> Slice(int offset, int length)
        {
            if ((uint)offset + (uint)length > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new NativeViewPtr<T>(it.Ptr + offset, it.Offset + offset, length);
        }

        public NativeViewPtr<T> SliceFrom(int offset)
        {
            if ((uint)offset > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new NativeViewPtr<T>(it.Ptr + offset, offset, it.Length - offset);
        }

        public Span<T> AsSpan(int offset = 0)
        {
            if ((uint)offset > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new Span<T>(it.Ptr + offset, it.Length - offset);
        }

        public Span<T> AsSpan(int offset, int length)
        {
            if ((uint)offset + (uint)length > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new Span<T>(it.Ptr + offset, length);
        }

        public void CopyTo(NativeViewPtr<T> dest, int srcOffset = 0, int dstOffset = 0, int count = -1)
        {
            if (count < 0) count = it.Length - srcOffset;

            if ((uint)srcOffset + (uint)count > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(srcOffset));

            if ((uint)dstOffset + (uint)count > (uint)dest.Length)
                throw new ArgumentOutOfRangeException(nameof(dstOffset));

            Unsafe.CopyBlockUnaligned(dest + dstOffset, it.Ptr + srcOffset, (uint)(count * Unsafe.SizeOf<T>()));
        }

    }


    extension<T>(NativeArray<T> it) where T : unmanaged
    {
        public NativeViewPtr<T> Slice(int offset, int length = 0)
        {
            if ((uint)offset + (uint)length > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new NativeViewPtr<T>(it.Ptr + offset, offset, length > 0 ? length : it.Length - offset);
        }

        public Span<T> AsSpan(int offset = 0)
        {
            if ((uint)offset > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new Span<T>(it.Ptr + offset, it.Length - offset);
        }

        public Span<T> AsSpan(int offset, int length)
        {
            if ((uint)offset + (uint)length > (uint)it.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return new Span<T>(it.Ptr + offset, length);
        }
    }
}