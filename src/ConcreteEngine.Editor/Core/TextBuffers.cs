using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Theme;

namespace ConcreteEngine.Editor.Core;
/*
internal static unsafe class AllocBuilder
{
    private static readonly NativeViewPtr<byte>[] Pointers = new NativeViewPtr<byte>[32];
    private static bool _isActive = false;
    public static AllocBlockBuilder Create()
    {
        if (_isActive) throw new InvalidOperationException("Another builder is already active");
        _isActive = true;

        var span = Pointers.AsSpan();
        span.Clear();
        return new AllocBlockBuilder(span);
    }

    public static ArenaBlock* Build(AllocBlockBuilder builder, out Span<NativeViewPtr<byte>> pointers)
    {
        if (!_isActive) throw new InvalidOperationException("Builder is not active");
        var block = TextBuffers.PersistentArena.Alloc(builder.Cursor);
        for (int i = 0; i < builder.Index; i++)
        {
            ref var viewPtr = ref Pointers[i];
            viewPtr = block->AllocSlice(viewPtr.Length);
        }

        pointers = Pointers.AsSpan(0,builder.Index);
        _isActive = false;
        return block;
    }
}

internal unsafe ref struct AllocBlockBuilder(Span<NativeViewPtr<byte>> pointers)
{
    public readonly Span<NativeViewPtr<byte>> Pointers = pointers;
    public int Index { get; private set; }
    public int Cursor { get; private set; }

    public NativeViewPtr<byte> AllocSlice(int sizeInBytes)
    {
        var offset = Cursor;
        Cursor += sizeInBytes;
        return Pointers[Index++] = new NativeViewPtr<byte>(null, offset, sizeInBytes);
    }
}
*/
internal unsafe ref struct SliceBuilder(Span<NativeViewPtr<byte>> entries)
{
    private readonly Span<NativeViewPtr<byte>> _entries = entries;
    private int _count;
    private int _cursor;

    public ref NativeViewPtr<byte> Next(int size)
    {
        int i = _count++;
        _entries[i] = new NativeViewPtr<byte>(null, _cursor, size);
        _cursor += size;
        return ref _entries[i];
    }

    public void Commit(ArenaAllocator allocator)
    {
        ArenaBlock* block = allocator.Alloc(_cursor);
        for (int i = 0; i < _count; i++)
        {
            ref var e = ref _entries[i];
            e.Ptr = block->AllocSlice(e.Length);
        }
    }
}
internal static unsafe class TextBuffers
{
    public static NativeArray<byte> StyleBuffer;
    public static NativeArray<byte> LogBuffer;

    private static NativeViewPtr<byte> _writerPtr;
    public static ArenaAllocator PersistentArena = null!;

    public static UnsafeSpanWriter GetWriter() => new(_writerPtr);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocateBuffers()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

        StyleBuffer = NativeArray.Allocate<byte>(StyleMap.GetSizeInBytes());
        StyleMap.Allocate(StyleBuffer);

        PersistentArena = new ArenaAllocator(1024 * 10);
        _writerPtr = PersistentArena.Alloc(256)->DataPtr;

        LogBuffer = NativeArray.Allocate<byte>(ConsoleService.LogStride * ConsoleService.StoredLogCap);
    }

    public static void Dispose()
    {
        StyleBuffer.Dispose();
        PersistentArena.Dispose();
        LogBuffer.Dispose();
    }
}