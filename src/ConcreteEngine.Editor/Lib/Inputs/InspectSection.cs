using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inputs;

internal sealed unsafe class InspectorHeader
{
    public uint Icon;
    public Color4 Color;
    public NativeString InspectorName;

    public InspectorHeader(NativeString inspectorName, uint icon, Color4 color)
    {
        ArgumentNullException.ThrowIfNull(inspectorName.TextStart);
        ArgumentOutOfRangeException.ThrowIfLessThan(inspectorName.Length, 2);

        InspectorName = inspectorName;
        Icon = icon;
        Color = color;
    }

    public void Draw()
    {
        ImGui.AlignTextToFramePadding();
        AppLayout.PushFontIconLarge();
        AppDraw.IconColored(Color, Icon);
        ImGui.PopFont();
        ImGui.SameLine(0, 8f);
        AppDraw.Text(InspectorName);
    }
}

internal sealed unsafe class InspectSection
{
    const float LabelFieldGap = 8.0f;
    const float LabelMinWidth = 90.0f;
    const float LabelMaxWidth = 180.0f;
    const float FieldMaxWidth = 220f;

    private static int _idCounter;

    public readonly int Id;
    private readonly uint Icon;

    private readonly float _labelWidth;

    private readonly NativeString _title;
    private readonly InputField[] _fields;

    private readonly Action _refresh;

    private FrameAccumulator _accumulator;

    public InspectSection(string title, InputField[] fields, Action refresh)
    {
        Id = ++_idCounter;
        _fields = fields;
        _refresh = refresh;
        _title = StringArena.AllocateStringId(title.Truncate(28), "title", Id);

        SetFetchRateMedium();

        float labelWidth = 0;
        AppLayout.PushFontText();
        foreach (var it in fields)
        {
            var length = ImGui.CalcTextSize(it.Label.AsTextSpan()).X;
            labelWidth = float.Max(labelWidth, length);
        }

        ImGui.PopFont();

        _labelWidth = float.Clamp(labelWidth, LabelMinWidth, LabelMaxWidth);
    }

    public void SetFetchRateHigh()
    {
        _accumulator = new FrameAccumulator(1f / 20f);
        _accumulator.Accumulator = _accumulator.TickDt;
    }

    public void SetFetchRateMedium()
    {
        _accumulator = new FrameAccumulator(1f / 8f);
        _accumulator.Accumulator = _accumulator.TickDt;
    }

    public void SetFetchRateLow()
    {
        _accumulator = new FrameAccumulator(1f);
        _accumulator.Accumulator = _accumulator.TickDt;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fetch(float dt)
    {
        _accumulator.Accumulate(dt);
        if (_accumulator.DrainTick()) _refresh();
    }

    public void Draw(float contentWidth)
    {
        var open = ImGui.CollapsingHeader(_title, ImGuiTreeNodeFlags.DefaultOpen);
        if (!open) return;

        var fieldWidth = contentWidth - _labelWidth - LabelFieldGap;
        fieldWidth = float.Min(fieldWidth, FieldMaxWidth);

        var labelSize = new Vector2(_labelWidth, ImGui.GetFrameHeight());

        ImGui.PushItemWidth(fieldWidth);
        foreach (var field in _fields)
        {
            ImGui.Selectable(field.Label, false, ImGuiSelectableFlags.None, labelSize);
            ImGui.SameLine(0, LabelFieldGap);
            field.Draw();
        }

        ImGui.PopItemWidth();
    }
}