using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;


internal struct EnumCombo<T>(int index, ImGuiComboFlags flags = ImGuiComboFlags.HeightLargest)
    where T : unmanaged, Enum
{
    public int Index = index;
    public readonly ImGuiComboFlags Flags = flags;

    public bool Draw(ReadOnlySpan<byte> label,ReadOnlySpan<byte> placeHolder, out T result, ref SpanWriter sw)
    {
        var names = EnumCache<T>.GetNames();
        var values = EnumCache<T>.GetValues();
        var index = Index;
        
        var preview = index < 0 ? placeHolder : sw.Write(names[index]);
        if (!ImGui.BeginCombo(label, preview, Flags))
        {
            result = default!;
            return false;
        }
        
        var changed = false;
        result = default!;
        for (var i = 0; i < names.Length; i++)
        {
            var isSelected = i == index;
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