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
        new(EnumCache<T>.Names, EnumCache<T>.Values, flags, start, defaultName);


    private string GetName(int index)
    {
        if (index == 0 && DefaultName is { } defaultName) return defaultName;
        return _names[index];
    }

    private bool Begin(int index, StrWriter8 sw)
    {
        var preview = (uint)index < _names.Length ? GetName(index) : Placeholder;

        ref var labelUtf8 = ref sw.Start(Label).Append("##combo"u8).End();
        sw.SetCursor(64);
        ref var previewUtf8 = ref sw.Append(preview).End(64);

        ImGui.PushID(Id);
        if (ImGui.BeginCombo(ref labelUtf8, ref previewUtf8, _flags))
            return true;

        ImGui.PopID();
        return false;
    }

    public bool Draw(int index, StrWriter8 sw, out T result)
    {
        var len = _names.Length;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)len, nameof(index));

        result = default!;

        if (!Begin(index, sw)) return false;

        var changed = false;
        var values = _values;
        for (var i = _start; i < len; i++)
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