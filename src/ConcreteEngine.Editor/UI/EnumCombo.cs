using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal struct EnumCombo<T>(int index, ImGuiComboFlags flags = ImGuiComboFlags.HeightLargest)
    where T : unmanaged, Enum
{
    public int Index = index;
    public readonly ImGuiComboFlags Flags = flags;

    private static string DefaultPlaceholder => "Select...";

    public bool Draw(string label, out T result) => Draw(label, DefaultPlaceholder, out result);

    public bool Draw(string label, string placeholder, out T result)
    {
        result = default!;
        var names = EnumCache<T>.GetNames();
        var values = EnumCache<T>.GetValues();

        var sw1 = StrUtils.Writer1();
        var sw2 = StrUtils.Writer2();

        var preview = Index < 0 ? placeholder : names[Index];
        if (!ImGui.BeginCombo(sw1.Write(label), sw2.Write(preview), Flags))
            return false;

        var changed = false;
        for (var i = 0; i < names.Length; i++)
        {
            var isSelected = i == Index;
            if (ImGui.Selectable(sw1.Write(names[i]), isSelected))
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