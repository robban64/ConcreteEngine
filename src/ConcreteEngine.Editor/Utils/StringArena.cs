using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;
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