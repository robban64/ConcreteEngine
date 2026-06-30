using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Utils;

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
    private readonly ArenaAllocator _arena;

    private StringArena()
    {
        const int blockCount = 4;
        _arena = new ArenaAllocator(CapacityUtils.PageSize * blockCount);
        _arena.AllocBlock(CapacityUtils.PageSize);
    }

    public NativeString Alloc(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        capacity = IntMath.AlignUp(capacity, 4);

        var sizeInBytes = Unsafe.SizeOf<NativeString.NativeStringData>() + capacity;
        if (!_arena.CanAlloc(sizeInBytes))
        {
            _arena.AllocBlock(CapacityUtils.PageSize);
            Logger.LogString(LogScope.Editor, $"StringArena - Allocated new block");
        }
        
        var memory = _arena.Tail.GetAllocator().AllocSlice(sizeInBytes);
        return NativeString.From(memory);
    }

    public NativeString AllocString(ReadOnlySpan<char> value)
    {
        var str = Alloc(Encoding.UTF8.GetByteCount(value) + 1);
        str.Set(value);
        return str;
    }

}
/*
internal struct StringHandle(byte blockId, byte slot)
{
    public readonly byte BlockId = blockId;
    public readonly byte Slot = slot;
}

internal sealed class StringArena
{
    public const int BlockSize = 1024;
    public const int DefaultStrLen = 64;
    //public const int StringsPerBlock = BlockSize / StringLength;

    private readonly ArenaAllocator _arena;

    private readonly StringBlock[] _stringBlocks;

    public StringArena()
    {
        _arena = new ArenaAllocator(BlockSize * 2);
        var memory = _arena.AllocBlock(BlockSize);
        _stringBlocks = [new StringBlock(0, DefaultStrLen, memory)];
    }

    public StringHandle AllocString(ReadOnlySpan<char> text = default)
    {
        var handle = _stringBlocks[0].AllocSlot();
        if (text.Length > 0)
            _stringBlocks[0].GetString(handle).Writer().Write(text);

        return handle;
    }

    private sealed class StringBlock
    {
        public readonly byte Id;
        public readonly MemoryBlockPtr Memory;
        public readonly byte[] Slots;
        public readonly byte StringCapacity;

        public StringHandle AllocSlot()
        {
            var slot = Slots.IndexOf((byte)0);
            if (slot == -1) ;

            var view = Memory.SliceData(new RangeU16(slot * StringCapacity, StringCapacity));
            view.Clear();
            return new StringHandle(Id, (byte)slot);
        }

        public NativeView<byte> GetString(StringHandle handle) =>
            Memory.SliceData(new RangeU16(handle.Slot * StringCapacity, StringCapacity));

        public StringBlock(byte id, byte stringLength, MemoryBlockPtr memory)
        {
            if (memory.IsNull || memory.Length == 0) Throwers.NullPointer(nameof(memory));
            if (!IntMath.IsPowerOfTwo(stringLength)) Throwers.InvalidArgument(nameof(stringLength));

            var slotLength = memory.Length / stringLength;

            Id = id;
            Memory = memory;
            Slots = new byte[slotLength];
            StringCapacity = stringLength;
        }
    }
}*/