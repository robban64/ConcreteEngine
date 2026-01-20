using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal class EnumCombo<T> : Widget where T : unmanaged, Enum
{
    private const string DefaultPlaceholder = "Select...";

    public string Label = string.Empty;

    private readonly ImGuiComboFlags _flags;
    private readonly int _start;

    private readonly string[] _names;
    private readonly T[] _values;

    public EnumCombo(ImGuiComboFlags flags = ImGuiComboFlags.None, int start = 0)
        : this(Enum.GetNames<T>(), Enum.GetValues<T>(), flags, start)
    {
    }

    public EnumCombo(string[] names, T[] values, ImGuiComboFlags flags = ImGuiComboFlags.None,
        int start = 0)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(names.Length, nameof(names));
        ArgumentOutOfRangeException.ThrowIfNotEqual(names.Length, values.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(start, names.Length);

        _flags = flags;
        _start = start;
        _names = names;
        _values = values;
    }

    public static EnumCombo<T> MakeFromCache()
    {
        return new EnumCombo<T>(EnumCache<T>.GetNames().ToArray(), EnumCache<T>.GetValues().ToArray());
    }

    public bool Draw(int index, out T result) => Draw(index, DefaultPlaceholder, out result);

    public bool Draw(int index, string placeholder, out T result)
    {
        result = default!;

        var names = _names;
        var values = _values;
        var sw = GetWriter1();
        var sw2 = GetWriter2();

        var preview = (uint)index < names.Length ? names[index] : placeholder;
        ImGui.PushID(Id);
        if (!ImGui.BeginCombo(sw2.Start(Label).Append("##combo").End(), sw.Write(preview), _flags))
        {
            ImGui.PopID();
            return false;
        }

        var changed = false;
        for (var i = int.Min(_start, index); i < names.Length; i++)
        {
            var isSelected = i == index;
            if (ImGui.Selectable(sw.Write(names[i]), isSelected))
            {
                index = i;
                result = values[i];
                changed = true;
            }

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
        ImGui.PopID();

        return changed;
    }
}