using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Data;

internal sealed class StringArena : IDisposable
{
    public static StringArena Instance { get; private set; } = null!;

    public static void Create()
    {
        if (Instance != null!) Throwers.InvalidOperation("StringArena already created");
        Instance = new StringArena();
    }


    public static int Remaining => Instance._allocator.Remaining;

    //
    private const int MaxBlocks = 4;

    private int _blockCount = 1;
    private readonly BumpAllocator _allocator;

    private StringArena()
    {
        _allocator = new BumpAllocator(CapacityUtils.PageSize * MaxBlocks, CapacityUtils.PageSize, 0, false);
        _allocator.AllocBlock(CapacityUtils.PageSize, true);
    }


    public NativeString AllocString(ReadOnlySpan<char> value, int extraCapacity = 0)
    {
        var str = AllocString(Encoding.UTF8.GetByteCount(value) + 1 + extraCapacity);
        str.Set(value);
        return str;
    }

    public NativeString AllocString(ReadOnlySpan<byte> value, int extraCapacity = 0)
    {
        var str = AllocString(value.Length + 1 + extraCapacity);
        str.Set(value);
        return str;
    }

    public NativeString AllocString(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        capacity = IntMath.AlignUp(capacity, 4);

        var sizeInBytes = NativeString.HeaderStride + capacity;
        Ensure(sizeInBytes);

        var memory = _allocator.Tail.AllocSlice(sizeInBytes);
        return NativeString.From(memory);
    }

    public NativeView<byte> AllocBytes(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        var sizeInBytes = IntMath.AlignUp(capacity, 4);
        Ensure(sizeInBytes);
        return _allocator.Tail.AllocSlice(sizeInBytes);
    }

    private void Ensure(int sizeInBytes)
    {
        if (sizeInBytes <= _allocator.Tail.Remaining) return;

        if (_blockCount++ > MaxBlocks) Throwers.InvalidOperation("Too many blocks");

        _allocator.AllocBlock(CapacityUtils.PageSize, true);
        Logger.Log(LogScope.Editor, $"StringArena - Allocated new block");
    }

    public void Dispose() => _allocator.Dispose();

    //
    public static NativeString AllocateString(int value) => Instance.AllocString(value);

    public static NativeString AllocateString(ReadOnlySpan<char> value, int extraCapacity = 0) =>
        Instance.AllocString(value, extraCapacity);

    public static NativeString AllocateString(ReadOnlySpan<byte> value, int extraCapacity = 0) =>
        Instance.AllocString(value, extraCapacity);
}