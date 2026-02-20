using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Widgets;

internal class EnumCombo<T> : Widget where T : unmanaged, Enum
{
    public readonly byte[] Label;
    private readonly byte[][] _names;

    private readonly int _start;

    private readonly T[] _values;

    public EnumCombo(string[] names, T[] values, int start = 0, string? defaultName = null, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(names.Length, nameof(names));
        ArgumentOutOfRangeException.ThrowIfNotEqual(names.Length, values.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(start, names.Length);

        _start = start;
        _values = values;
        
        _names = UtfText.ToUtf8ByteArrays(names);
        if (defaultName != null)
            _names[0] = Encoding.UTF8.GetBytes(defaultName);


        Label = label != null ? Encoding.UTF8.GetBytes(label) : DefaultLabel;
        
    }
    
    public EnumCombo(int start = 0, string? defaultName = null, string? label = null)
        : this(Enum.GetNames<T>(), Enum.GetValues<T>() , start, defaultName, label)
    {
    }

    public static EnumCombo<T> MakeFromCache(int start = 0, string? defaultName = null, string? label = null) =>
        new(EnumCache<T>.Names, EnumCache<T>.Values, start, defaultName,label);


    private bool Begin(int index, ImGuiComboFlags flags = 0)
    {
        var name = (uint)index < _names.Length ? _names[index] : PlaceholderEmpty();
        ImGui.PushID(Id);
        if (ImGui.BeginCombo(Label, name, flags))
            return true;

        ImGui.PopID();
        return false;
    }

    public bool Draw(int index, out T result, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        result = default!;
        if (!Begin(index, flags)) return false;

        var changed = false;
        var values = _values;
        var names = _names;
        var len = int.Min(names.Length, values.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)len, nameof(index));

        var start = int.Max(0, _start);
        for (var i = start; i < len; i++)
        {
            var isSelected = i == index;
            if (ImGui.Selectable(names[i], isSelected))
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