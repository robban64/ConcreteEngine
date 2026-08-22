using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Utils;

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
        _allocator = new BumpAllocator(CapacityUtils.PageSize * MaxBlocks, CapacityUtils.PageSize, 0, true);
        _allocator.AllocBlock(CapacityUtils.PageSize, true);
    }
    
    public NativeView<byte> AllocBytes(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        var sizeInBytes = capacity + 1;
        Ensure(sizeInBytes);
        var memory = _allocator.Tail.AllocSlice(sizeInBytes);
        return memory.Slice(0, memory.Length - 1);
    }

    public NativeString AllocString(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        var sizeInBytes = NativeString.HeaderStride + capacity + 1;
        Ensure(sizeInBytes);
        var memory = _allocator.Tail.AllocSlice(sizeInBytes);
        return NativeString.Create(memory.Slice(0, memory.Length - 1));
    }

    public NativeString AllocString(ReadOnlySpan<char> value, int extraCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfZero(value.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(extraCapacity);
        var str = AllocString(Encoding.UTF8.GetByteCount(value) + extraCapacity);
        str.Set(value);
        return str;
    }

    public NativeString AllocString(ReadOnlySpan<byte> value, int extraCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfZero(value.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(extraCapacity);
        var str = AllocString(value.Length + extraCapacity);
        str.Set(value);
        return str;
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
    public static NativeString AllocateString(int sizeInBytes) => Instance.AllocString(sizeInBytes);

    public static NativeString AllocateString(ReadOnlySpan<char> value, int extraCapacity = 0) =>
        Instance.AllocString(value, extraCapacity);

    public static NativeString AllocateString(ReadOnlySpan<byte> value, int extraCapacity = 0) =>
        Instance.AllocString(value, extraCapacity);

    public static NativeString AllocateStringId(ReadOnlySpan<char> value, ReadOnlySpan<char> strId, int? intId = null)
    {
        var capacity = value.Length + 2 + strId.Length;
        if(intId.HasValue) capacity += IntMath.GetDigits(intId.Value);
        
        var str = Instance.AllocString(capacity);
        var sw = str.GetWriter();
        sw.Append(value);
        sw.AppendAscii('#', '#');
        sw.Append(strId);
        if(intId.HasValue) sw.Append(intId.Value);
        sw.EndNativeString();
        return str;
    }
}