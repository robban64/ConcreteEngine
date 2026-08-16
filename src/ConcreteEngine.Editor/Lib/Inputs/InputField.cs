using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;

internal abstract unsafe class InputField
{
    private static int _idCounter;

    public readonly NativeString Label;
    private readonly byte _stringIdStart;

    public readonly InputKind Kind;
    public InputTrigger Trigger = InputTrigger.OnChange;
    public LabelPlacement LabelPlacement = LabelPlacement.Top;

    protected byte* StringId => Label.TextStart + _stringIdStart;

    protected InputField(string label, InputKind kind)
    {
        Kind = kind;
        Label = CreateNativeLabel(label, ++_idCounter, out _stringIdStart);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DrawLabel() => DrawLabel(Label, LabelPlacement);

    protected bool ShouldTrigger()
    {
        return Trigger switch
        {
            InputTrigger.OnChange => true,
            InputTrigger.AfterChange => ImGui.IsItemDeactivatedAfterEdit(),
            InputTrigger.AfterChangeDeActive => ImGui.IsItemDeactivatedAfterEdit() && !ImGui.IsItemActive(),
            _ => false
        };
    }

    protected static void DrawLabel(NativeString label, LabelPlacement placement)
    {
        if (placement is LabelPlacement.Top)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SetNextItemWidth(GuiTheme.FormItemWidth);
        }
        else if (placement is LabelPlacement.Inline)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SameLine(GuiTheme.FormItemInlineOffset);
            ImGui.SetNextItemWidth(GuiTheme.FormItemInlineWidth);
        }
    }

    protected static NativeString CreateNativeLabel(string name, int id, out byte idStart)
    {
        var sw = ScratchBuffer.Writer().Append(name.Truncate(31)).Append((byte)0);
        idStart = (byte)sw.Cursor;
        return StringArena.AllocateString(sw.AppendImGuiId(id).EndSpan());
    }
}