using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Inspection;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;
/*
internal sealed unsafe class UiInputField
{
    private static int _idCounter = 0;

    private UiInput _input;

    public readonly int DrawId;
    public readonly string Label;

    public FieldLabelPlacement LabelPlacement = FieldLabelPlacement.Top;

    public UiInputField(string label, UiInput input)
    {
        Label = label;
        _input = input;
        DrawId = ++_idCounter;
    }

    public bool Draw()
    {
        return _input.Draw(DrawLabel());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte* DrawLabel()
    {
        var sw = TextBuffers.GetWriter();
        switch (LabelPlacement)
        {
            case FieldLabelPlacement.Top:
                sw.Append(Label);
                AppDraw.Text(sw.End());
                ImGui.Separator();
                ImGui.PushItemWidth(GuiTheme.FormItemWidth);
                break;
            case FieldLabelPlacement.Inline:
                sw.Append(Label);
                ImGui.PushItemWidth(GuiTheme.FormItemInlineWidth);
                break;
        }

        return sw.AppendImGuiId(DrawId).End();
    }
}



internal abstract unsafe class UiInput(InputFieldKind kind)
{
    public readonly InputFieldKind Kind = kind;
    public InputTrigger Trigger = InputTrigger.OnChange;

    public abstract bool IsValueType { get; }
    public abstract ref byte GetRawValue();
    public abstract bool Draw(byte* label);
}

internal sealed unsafe class FloatInput2 : UiInput
{
    private Float4 _value;
    public int Components;
    public float Speed, Min, Max;
    public String8Utf8 Format;

    private readonly delegate*<int, byte*, float*, byte*, float, float, float, bool> _drawFunc;

    public FloatInput2(InputFieldKind kind, float speed = 1f, float min = 0, float max = 0,
        string format = "%.2f") : base(kind)
    {
        _drawFunc = InputFieldDrawer.BindFloat(0);
        Format = format;
        Speed = speed;
        Min = min;
        Max = max;
    }

    public override bool IsValueType => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ref byte GetRawValue() => ref Unsafe.As<float, byte>(ref _value.Ref());

    public void SetValue<T>(T value) where T : IFloatValue => _value.From(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Draw(byte* label)
    {
        var value = _value;
        var format = Format;
        var changed = _drawFunc(Components, label, (float*)&value, (byte*)&format, Speed, Min, Max);
        if (changed) _value = value;
        return changed && ShouldTrigger();
    }

    private bool ShouldTrigger()
    {
        return Trigger switch
        {
            InputTrigger.OnChange => true,
            InputTrigger.AfterChange => ImGui.IsItemDeactivatedAfterEdit(),
            InputTrigger.AfterChangeDeactive => ImGui.IsItemDeactivatedAfterEdit() && !ImGui.IsItemActive(),
            _ => false
        };
    }
}

*/