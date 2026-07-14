using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Field;


internal abstract unsafe class InputField
{
    private static int _idCounter;

    protected NativeString _label;
    private readonly byte _stringIdStart;

    public readonly InputKind Kind;
    public InputTrigger Trigger = InputTrigger.OnChange;
    public LabelPlacement LabelPlacement = LabelPlacement.Top;

    protected byte* StringId => _label.TextStart + _stringIdStart;

    protected InputField(string label, InputKind kind)
    {
        Kind = kind;
        _label = WriteLabel(label,++_idCounter, out _stringIdStart);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DrawLabel()
    {
        if (LabelPlacement is LabelPlacement.Top)
        {
            ImGui.TextUnformatted(_label.TextStart);
            ImGui.Separator();
            ImGui.SetNextItemWidth(GuiTheme.FormItemWidth);
        }
        else if (LabelPlacement is LabelPlacement.Inline)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(_label.TextStart);
            ImGui.SameLine(GuiTheme.FormItemInlineOffset);
            ImGui.SetNextItemWidth(GuiTheme.FormItemInlineWidth);
        }
    }

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
    
    protected static NativeString WriteLabel(string name, int id, out byte idStart)
    {
        var sw = ScratchBuffer.Writer().Append(name.Truncate(31)).Append((byte)0);
        idStart = (byte)sw.Cursor;
        return StringArena.AllocateString(sw.AppendImGuiId(id).EndSpan());
    }

}