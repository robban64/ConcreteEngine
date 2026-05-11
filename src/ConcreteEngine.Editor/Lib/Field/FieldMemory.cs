using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using static ConcreteEngine.Core.Common.Memory.ArenaAllocator;

namespace ConcreteEngine.Editor.Lib.Field;

internal sealed unsafe class FieldMemory
{
    private MemoryBlockPtr _memory = null;

    public RangeU16 LabelHandle;
    public RangeU16 TextLabelHandle;
    public RangeU16 IdLabelHandle;
    public RangeU16 ValueHandle;
    public RangeU16 CustomDataHandle;

    public bool IsNull => _memory == null;

    public void Allocate(ArenaAllocator allocator, int id, string name, int valueSize, int customDataSize = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(valueSize, 4);

        var nameLength = Encoding.UTF8.GetByteCount(name);
        var labelLength = nameLength + IntMath.GetDigits(id) + 2 + 1;

        var builder = allocator.MakeBuilder();
        
        var labelView = builder.AllocSlice(labelLength);
        labelView.Writer().Append(name).Append("##input").Append(id);

        LabelHandle = labelView.AsRange16();
        TextLabelHandle = labelView.Slice(0, nameLength).AsRange16();
        IdLabelHandle = labelView.SliceFrom(nameLength).AsRange16();
        ValueHandle = builder.AllocSlice(valueSize).AsRange16();

        if (customDataSize > 0)
        {
            CustomDataHandle = builder.AllocSlice(customDataSize).AsRange16();
        }

        _memory = allocator.CommitBuilder(builder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* GetValue<T>() where T : unmanaged, IFieldValue => (T*)(_memory.Data + ValueHandle.Offset);

    public NativeView<byte> FullLabelStr => _memory.Data.Slice(LabelHandle);
    public NativeView<byte> TextLabelStr => _memory.Data.Slice(TextLabelHandle);
    public NativeView<byte> IdLabelStr => _memory.Data.Slice(IdLabelHandle);
    public NativeView<byte> CustomData => _memory.Data.Slice(CustomDataHandle);
}