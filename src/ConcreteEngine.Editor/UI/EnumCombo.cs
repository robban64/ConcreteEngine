using ConcreteEngine.Core.Common.Memory;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal class EnumCombo<T>
    where T : unmanaged, Enum
{
    private const string DefaultPlaceholder = "Select...";

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

    public bool Draw(int index, ReadOnlySpan<byte> label, out T result) =>
        Draw(index, label, DefaultPlaceholder, out result);

    public bool Draw(int index, ReadOnlySpan<byte> label, string placeholder, out T result)
    {
        result = default!;

        var names = _names;
        var values = _values;
        var sw = Widgets.GetWriter1();

        var preview = (uint)index < (uint)names.Length ? names[index] : placeholder;
        if (!ImGui.BeginCombo(label, sw.Write(preview), _flags))
            return false;

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
        return changed;
    }
}