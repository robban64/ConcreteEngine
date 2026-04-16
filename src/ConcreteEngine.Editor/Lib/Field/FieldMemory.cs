using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;
using static ConcreteEngine.Core.Common.Memory.ArenaAllocator;

namespace ConcreteEngine.Editor.Lib.Field;


internal sealed unsafe class FieldMemory
{
    public ArenaBlockPtr Memory = null;
    public Range32 LabelHandle;
    public Range32 TextLabelHandle;
    public Range32 ValueHandle;
    public Range32 CustomDataHandle;
    
    public bool IsNull => Memory == null;

    public void Allocate(ArenaBlockBuilder builder, int id, string name, int valueSize, int customDataSize = 0) 
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(valueSize, 4);

        var nameLength = Encoding.UTF8.GetByteCount(name);
        var labelLength = nameLength + IntMath.GetDigits(id) + 2 + 1;

        var labelView = builder.AllocSlice(labelLength);
        labelView.Writer().Append(name).Append("##input").Append(id);
        
        LabelHandle = labelView.AsRange32();
        TextLabelHandle = labelView.Slice(0, nameLength).AsRange32();
        ValueHandle = builder.AllocSlice(valueSize).AsRange32();

        if(customDataSize > 0)
        {
            CustomDataHandle = builder.AllocSlice(customDataSize).AsRange32();
        }
        Memory = builder.Commit();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* GetValue<T>() where T : unmanaged, IFieldValue => (T*)(Memory.DataPtr + ValueHandle.Offset);

    public NativeView<byte> FullLabelStr => Memory.DataPtr.Slice(LabelHandle);
    public NativeView<byte> TextLabelStr => Memory.DataPtr.Slice(TextLabelHandle);
    public NativeView<byte> IdLabelStr =>   Memory.DataPtr.Slice(TextLabelHandle.Length, LabelHandle.Length);
    public NativeView<byte> CustomData =>   Memory.DataPtr.Slice(CustomDataHandle);

}
