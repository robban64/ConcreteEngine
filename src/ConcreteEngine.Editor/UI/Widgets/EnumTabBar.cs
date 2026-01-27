using System.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal class EnumTabBar<T> : Widget where T : unmanaged, Enum
{
    public int Index;
    public ImGuiTabBarFlags Flags;
    public Vector2 FramePadding = new(12, 4);

    private readonly string[] _names;
    private readonly T[] _values;

    public EnumTabBar(int index = -1, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
        : this(Enum.GetNames<T>(), Enum.GetValues<T>(), index, flags)
    {
    }

    public EnumTabBar(string[] names, T[] values, int index = -1, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(names.Length, nameof(names));
        ArgumentOutOfRangeException.ThrowIfNotEqual(names.Length, values.Length);
        _names = names;
        _values = values;
        Index = index;
        Flags = flags;
    }

    public bool Draw(StrWriter8 sw, out T value)
    {
        value = default;
        var names = _names;
        var values = _values;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, FramePadding);
        ImGui.PushID(Id);
        var changed = false;
        if (ImGui.BeginTabBar("##tabs"u8, Flags))
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (!ImGui.BeginTabItem(ref sw.Write(names[i]))) continue;

                if (Index != i)
                {
                    Index = i;
                    value = values[i];
                    changed = true;
                }

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        ImGui.PopID();
        ImGui.PopStyleVar(1);

        return changed;
    }
}