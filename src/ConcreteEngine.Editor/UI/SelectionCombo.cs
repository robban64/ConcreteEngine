using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class SelectionCombo<T> : Widget where T : IEquatable<T>
{
    private const ImGuiComboFlags Flags = ImGuiComboFlags.HeightLargest;
    private static readonly string Placeholder = "Select...";

    private readonly string[] _names;
    private readonly T[] _values;

    private int _index;

    public SelectionCombo(string[] names, T[] values)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(values.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(names.Length, values.Length);

        _names = names;
        _values = values;
    }

    public void Sync(T value)
    {
        var values = _values;
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i].Equals(value))
            {
                _index = i;
                return;
            }
        }

        _index = -1;
    }

    public bool Draw(out T result)
    {
        result = default!;
        var index = _index;

        var names = _names;
        var values = _values;

        var sw = GetWriter2();
        var preview = (uint)_index < (uint)names.Length ? names[_index] : Placeholder;
        ImGui.PushID(Id);
        if (!ImGui.BeginCombo("##combo"u8, preview, Flags))
        {
            result = default!;
            ImGui.PopID();
            return false;
        }

        var changed = false;
        for (var i = 0; i < names.Length; i++)
        {
            var isSelected = i == index;
            if (ImGui.Selectable(sw.Write(names[i]), isSelected))
            {
                _index = i;
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