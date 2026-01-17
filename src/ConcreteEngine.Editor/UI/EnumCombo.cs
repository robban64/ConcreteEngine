using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal struct EnumCombo<T>(int index, ImGuiComboFlags flags = ImGuiComboFlags.HeightLargest)
    where T : unmanaged, Enum
{
    public int Index = index;
    public readonly ImGuiComboFlags Flags = flags;

    private const string DefaultPlaceholder = "Select...";

    public bool Draw(ref SpanWriter sw, ReadOnlySpan<byte> label, out T result) =>
        Draw(ref sw, label, DefaultPlaceholder, out result);

    public bool Draw(ref SpanWriter sw, ReadOnlySpan<byte> label, string placeholder, out T result)
    {
        result = default!;
        var names = EnumCache<T>.GetNames();
        var values = EnumCache<T>.GetValues();

        var preview = (uint)Index < (uint)names.Length ? names[Index] : placeholder;
        
        if (!ImGui.BeginCombo(label, sw.Write(preview), Flags))
            return false;

        var changed = false;
        for (var i = 0; i < names.Length; i++)
        {
            var isSelected = i == Index;
            if (ImGui.Selectable(sw.Write(names[i]), isSelected))
            {
                Index = i;
                result = values[i];
                changed = true;
            }

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
        return changed;
    }
}