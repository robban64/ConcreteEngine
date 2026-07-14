using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Core.Data;

internal sealed class StringArena : IDisposable
{
    public static StringArena Instance { get; private set; } = null!;

    public static void Create()
    {
        if (Instance != null!) Throwers.InvalidOperation("StringArena already created");
        Instance = new StringArena();
    }

    public static NativeString AllocateString(int value) => Instance.AllocString(value);

    public static NativeString AllocateString(ReadOnlySpan<char> value, int extraCapacity = 0)
        => Instance.AllocString(value, extraCapacity);

    public static NativeString AllocateString(ReadOnlySpan<byte> value, int extraCapacity = 0)
        => Instance.AllocString(value, extraCapacity);

    public static int Remaining => Instance._arena.Remaining;

    //
    private const int MaxBlocks = 4;

    private int _blockCount = 1;
    private readonly ArenaAllocator _arena;

    private StringArena()
    {
        _arena = new ArenaAllocator(CapacityUtils.PageSize * MaxBlocks);
        _arena.AllocBlock(CapacityUtils.PageSize);
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

        var sizeInBytes = Unsafe.SizeOf<NativeString.NativeStringHeader>() + capacity;
        Ensure(sizeInBytes);

        var memory = _arena.Tail.GetAllocator().AllocSlice(sizeInBytes);
        return NativeString.From(memory);
    }
    
    public NativeView<byte> AllocRaw(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        var sizeInBytes = IntMath.AlignUp(capacity, 4);
        Ensure(sizeInBytes);
        return _arena.Tail.GetAllocator().AllocSlice(sizeInBytes);
    }


    private void Ensure(int sizeInBytes)
    {
        if (_arena.CanAlloc(sizeInBytes))return;

        if (_blockCount++ > MaxBlocks) Throwers.InvalidOperation("Too many blocks");

        _arena.AllocBlock(CapacityUtils.PageSize);
        Logger.Log(LogScope.Editor, $"StringArena - Allocated new block");

    }
    public void Dispose() => _arena.Dispose();
}