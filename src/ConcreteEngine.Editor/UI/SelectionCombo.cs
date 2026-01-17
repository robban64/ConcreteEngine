using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class SelectionCombo<T> where T : IEquatable<T>
{
    private readonly string[] _names;
    private readonly T[] _values;

    public int Index;
    public string Placeholder = "Select...";
    
    public ImGuiComboFlags Flags = ImGuiComboFlags.HeightLargest;

    public SelectionCombo(string[] names, T[] values)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);
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
                Index = i;
                return;
            }
        }

        Index = -1;
    }

    public bool Draw(string label, out T result)
    {
        var names = _names;
        var values = _values;
        var sw = StrUtils.WidgetSw1();
        result = default!;
        
        var index = Index;
        var preview = index < 0 ? Placeholder : names[index];
        if (!ImGui.BeginCombo(sw.Write(label), preview, Flags))
        {
            result = default!;
            return false;
        }

        var changed = false;
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
