using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

public struct TextStyle(
    in Color4 color,
    float fontSize = 0f,
    float sameLineSpacing = 0f)
{
    public Color4 Color = color;
    public float SameLineSpacing = sameLineSpacing;
    public float FontSize = fontSize;
    public bool HasColor = true;
    public static readonly TextStyle Default = default;
}

public static class TextFieldFormatter
{
    internal static UnsafeSpanWriter Sw;
    public static void SizeAspectFormat(Size2D size)
    {
        Sw.Start(size.Width).Append('x').Append(size.Height).Append('[').Append(size.AspectRatio, "F2").Append(']');
    }

    public static void IdAndSubjectFormat((int id, string subject) value)
    {
        Sw.Start(value.subject).Append(" [").Append(value.id).Append("]");
    }

    public static void IdAndGenFormat((int id, long gen) value)
    {
        Sw.Start(" [").Append(value.id).Append(':').Append(value.gen).Append("]");
    }
}

//public delegate void TextFieldFormatterDel<in TValue>(TValue value);

public sealed class ValueTextField<TValue>(string name, Func<TValue> getter, Action<TValue> formatter)
    : PropertyField(name)
{
    private String16Utf8 _value;

    public void Refresh()
    {
        formatter(getter());
        _value = new String16Utf8(TextFieldFormatter.Sw.EndSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte Get()
    {
        if (Stepper.Tick())
        {
            formatter(getter());
            _value = new String16Utf8(TextFieldFormatter.Sw.EndSpan());
        }

        return ref _value.GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Draw()
    {
        ref var value = ref  Get();
        ImGui.TextUnformatted(ref NameUtf8.GetRef());
        ImGui.SameLine();
        ImGui.TextUnformatted(ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Draw(in Color4 color)
    {
        ref var value = ref  Get();

        ImGui.TextUnformatted(ref NameUtf8.GetRef());
        ImGui.SameLine();
        ImGui.TextColored(color, ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Draw(in TextStyle style)
    {
        ref var value = ref  Get();

        ImGui.TextUnformatted(ref NameUtf8.GetRef());
        ImGui.SameLine(style.SameLineSpacing);

        if (style.FontSize > 0)
            ImGui.PushFont(null, style.FontSize);

        if (style.HasColor)
            ImGui.PushStyleColor(ImGuiCol.Text, style.Color);

        ImGui.TextUnformatted(ref value);

        if (style.HasColor)
            ImGui.PopStyleColor();

        if (style.FontSize > 0)
            ImGui.PopFont();
    }
    /*
        public void DrawInspectorObjectHeader(in Color4 color)
        {
            ImGui.TextUnformatted(ref Get());
            ImGui.SameLine();
            ImGui.PushFont(null, 15);
            ImGui.TextColored(color, ref _name.GetRef());
            ImGui.PopFont();
        }
    */
}
/*
public abstract class BaseValueTextField<TValue> : PropertyField
{
    internal String16Utf8 _name;
    internal String16Utf8 _value;
    private readonly Func<TValue> _getter;
    private FrameStepper _stepper;

    public BaseValueTextField(string name, Func<TValue> getter) : base(name)
    {
        _getter = getter;
    }

    public ref byte Get(UnsafeSpanWriter sw)
    {
        if (_stepper.Tick())
            _value = new String16Utf8(OnFormat(_getter(), sw));

        return ref _value.GetRef();
    }

    internal abstract ReadOnlySpan<byte> OnFormat(TValue value, UnsafeSpanWriter sw);
    internal abstract void OnDraw(ref byte name, ref byte value);

}

public sealed class BasicTextField<TValue>(string name, Func<TValue> getter)
    : BaseValueTextField<TValue>(name,getter) where TValue : IUtf8SpanFormattable
{
    internal override ReadOnlySpan<byte> OnFormat(TValue value, UnsafeSpanWriter sw) => sw.Start(value).EndSpan();

    internal override void OnDraw(ref byte name, ref byte value)
    {
        ImGui.TextUnformatted(ref name);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref value);

    }
}

public sealed class InspectorHeaderTextField(string name, Func<(int id, long gen)> getter)
    : BaseValueTextField<(int id, long gen)>(name,getter)
{
    internal override ReadOnlySpan<byte> OnFormat((int id, long gen) value, UnsafeSpanWriter sw)
    => sw.Start(" [").Append(value.id).Append(':').Append(value.gen).Append("]").EndSpan();

    internal void Draw(in Color4 color)
    {
        ImGui.TextUnformatted(ref _value.GetRef());
        ImGui.SameLine();
        ImGui.PushFont(null, 15);
        ImGui.TextColored(color, ref _name.GetRef());
        ImGui.PopFont();
    }

    internal override void OnDraw(ref byte name, ref byte value) => throw new NotImplementedException();
}*/