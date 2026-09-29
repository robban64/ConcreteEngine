using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inputs;

internal abstract unsafe class InputField
{
    private static int _idCounter;

    protected readonly int Id;
    protected readonly String8Utf8 StringId;

    public readonly NativeString Label;

    public readonly InputKind Kind;
    public InputTrigger Trigger = InputTrigger.OnChange;

    protected InputField(ReadOnlySpan<char> label, InputKind kind)
    {
        Id = ++_idCounter;
        Kind = kind;
        Label = StringArena.AllocateStringId(label, "lbl", Id);

        String8Utf8 strId = default;
        var sw = new NativeSpanWriter((byte*)&strId, 8);
        sw.AppendAscii('#', '#').AppendAscii('i', 'f').Append(Id);
        strId._value[String8Utf8.TextLength] = 0;

        StringId = strId;
    }

    public abstract bool Draw();

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
}