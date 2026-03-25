
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;
/*
internal sealed unsafe class StringValueField : PropertyField
{

    private NativeViewPtr<byte> _value;
    private  Func<ReadOnlySpan<char>>? _getter;
    private  Action<Span<char>>? _setter;

    public int Capacity;

    public override int SizeInBytes => Capacity;

    public StringValueField(string name, FieldWidgetKind widgetKind, Func<byte>? getter = null, Action<byte>? setter= null)
    {
    }

    public override void Unbind() => throw new NotImplementedException();
    public override void Refresh() => throw new NotImplementedException();
    public override bool Draw()
    {
        return ImGui.InputText(ref GetLabel(), _value, (nuint)_value.Length);

    }

    protected override void OnAllocate(ArenaBlock* allocator)
    {
        _value = allocator->AllocSlice(Capacity);
    }

}*/