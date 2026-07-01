using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Core.Data;

internal sealed class StringArena
{
    public static StringArena Instance { get; private set; } = null!;
    
    public static void Create()
    {
        if(Instance != null!) Throwers.InvalidOperation("StringArena already created");
        Instance = new StringArena();
    }
    
    public static NativeString AllocateString(int value) => Instance.Alloc(value);
    public static NativeString AllocateString(ReadOnlySpan<char> value) => Instance.AllocString(value);

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

    public NativeString AllocString(ReadOnlySpan<char> value)
    {
        var str = Alloc(Encoding.UTF8.GetByteCount(value) + 1);
        str.Set(value);
        return str;
    }
    
    public NativeString Alloc(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        capacity = IntMath.AlignUp(capacity, 4);

        var sizeInBytes = Unsafe.SizeOf<NativeString.NativeStringHeader>() + capacity;
        if (!_arena.CanAlloc(sizeInBytes))
        {
            if(_blockCount++ > MaxBlocks) Throwers.InvalidOperation("Too many blocks");

            _arena.AllocBlock(CapacityUtils.PageSize);
            Logger.LogString(LogScope.Editor, $"StringArena - Allocated new block");
        }
        
        var memory = _arena.Tail.GetAllocator().AllocSlice(sizeInBytes);
        return NativeString.From(memory);
    }

}
