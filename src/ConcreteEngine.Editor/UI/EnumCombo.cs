using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal class EnumCombo<T> : Widget where T : unmanaged, Enum
{
    private const string DefaultPlaceholder = "Select...";

    public string Label = string.Empty;
    public string Placeholder = DefaultPlaceholder;

    public string DefaultName;

    private readonly ImGuiComboFlags _flags;
    private readonly int _start;

    private readonly string[] _names;
    private readonly T[] _values;

    public EnumCombo(ImGuiComboFlags flags = 0, int start = 0, string? defaultName = null)
        : this(Enum.GetNames<T>(), Enum.GetValues<T>(), flags, start, defaultName)
    {
    }

    public EnumCombo(string[] names, T[] values, ImGuiComboFlags flags = 0, int start = 0, string? defaultName = null)
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
        DefaultName = defaultName ?? _names[0];
    }

    public static EnumCombo<T> MakeFromCache(ImGuiComboFlags flags = 0, int start = 0, string? defaultName = null) =>
        new(EnumCache<T>.GetNames().ToArray(), EnumCache<T>.GetValues().ToArray(), flags, start, defaultName);


    private string GetName(int index)
    {
        if (index == 0 && DefaultName is { } defaultName) return defaultName;
        return _names[index];
    }

    public bool Draw(int index, StrWriter8 sw, out T result)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_names.Length, nameof(index));

        result = default!;

        var sw1 = sw.GetSlicedWriter(0);
        var sw2 = sw.GetSlicedWriter(64);

        var preview = (uint)index < _names.Length ? GetName(index) : Placeholder;
        ImGui.PushID(Id);
        if (!ImGui.BeginCombo(ref sw1.Start(Label).Append("##combo"u8).End(), ref sw2.Write(preview), _flags))
        {
            ImGui.PopID();
            return false;
        }

        var changed = false;
        var values = _values;
        for (var i = _start; i < _names.Length; i++)
        {
            var isSelected = i == index;
            var name = GetName(i);
            if (ImGui.Selectable(ref sw.Write(name), isSelected))
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