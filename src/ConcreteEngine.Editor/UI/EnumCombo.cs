using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal class EnumCombo<T> : Widget where T : unmanaged, Enum
{
    private const string DefaultPlaceholder = "Select...";

    public string Label = string.Empty;
    public string Placeholder = DefaultPlaceholder;

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

    public static EnumCombo<T> MakeFromCache() =>
        new(EnumCache<T>.GetNames().ToArray(), EnumCache<T>.GetValues().ToArray());

    public bool Draw(int index, SpanWriter sw, out T result)
    {
        result = default!;

        var names = _names;
        var values = _values;

        var sw1 = sw.GetSlicedWriter(0, 64);
        var sw2 = sw.GetSlicedWriter(64, 64);

        var preview = (uint)index < names.Length ? names[index] : Placeholder;
        ImGui.PushID(Id);
        if (!ImGui.BeginCombo(sw1.Start(Label).Append("##combo"u8).End(), sw2.Write(preview), _flags))
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