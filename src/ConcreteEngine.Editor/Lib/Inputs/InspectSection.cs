using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
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

    private readonly float _labelWidth;

    private readonly NativeString _title;
    private readonly InputField[] _fields;
    
    private readonly Action _refresh;

    public InspectSection(string title, InputField[] fields, Action refresh)
    {
        _title = StringArena.AllocateString(title.Truncate(32));
        _fields = fields;
        _refresh = refresh;

        float labelWidth = 0;
        AppLayout.PushFontText();
        foreach (var it in fields)
        {
            var length = ImGui.CalcTextSize(it.Label.AsSpan()).X;
            labelWidth = float.Max(labelWidth, length);
        }
        ImGui.PopFont();

        _labelWidth = float.Clamp(labelWidth, LabelMinWidth, LabelMaxWidth);
    }

    public void Draw(float contentWidth = 0)
    {
        if (!ImGui.CollapsingHeader(_title, ImGuiTreeNodeFlags.DefaultOpen)) return;

        _refresh();
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));

        if(contentWidth == 0) contentWidth= ImGui.GetContentRegionAvail().X;
        var fieldWidth = contentWidth - _labelWidth - LabelFieldGap;
        fieldWidth = float.Min(fieldWidth, FieldMaxWidth);
        ImGui.PushItemWidth(fieldWidth);
        
        var labelSize = new Vector2(_labelWidth, ImGui.GetFrameHeight());
        foreach (var field in _fields)
        {
            ImGui.Selectable(field.Label, false, ImGuiSelectableFlags.None, labelSize);
            ImGui.SameLine(0, LabelFieldGap);
            field.Draw();
        }
        ImGui.PopItemWidth();
        ImGui.PopStyleVar();

    }

    public void DrawSubSection()
    {
        _refresh();
        ImGui.SeparatorText(_title);
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));

        var contentWidth = ImGui.GetContentRegionAvail().X;
        var fieldWidth = contentWidth - _labelWidth - LabelFieldGap;
        fieldWidth = float.Min(fieldWidth, FieldMaxWidth);
        ImGui.PushItemWidth(fieldWidth);
        
        var labelSize = new Vector2(_labelWidth, ImGui.GetFrameHeight());
        foreach (var field in _fields)
        {
            ImGui.Selectable(field.Label, false, ImGuiSelectableFlags.None, labelSize);
            ImGui.SameLine(0, LabelFieldGap);
            field.Draw();
        }
        ImGui.PopItemWidth();
        ImGui.PopStyleVar();

    }

}