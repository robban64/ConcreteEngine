using System.Numerics;
using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Widgets;

internal class EnumTabBar<T> : Widget where T : unmanaged, Enum
{
    private readonly byte[][] _names;
    private readonly T[] _values;
    public int Index;

    public EnumTabBar(int index = -1)
        : this(Enum.GetNames<T>(), Enum.GetValues<T>(), index)
    {
    }

    public EnumTabBar(string[] names, T[] values, int index = -1)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(names.Length, nameof(names));
        ArgumentOutOfRangeException.ThrowIfNotEqual(names.Length, values.Length);
        _names = UtfText.ToUtf8ByteArrays(names);
        _values = values;
        Index = index;
    }

    public bool Draw(out T value, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
    {
        value = default;
        var changed = false;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
        ImGui.PushID(Id);

        if (!ImGui.BeginTabBar("##tabs"u8, flags))
        {
            ImGui.PopID();
            ImGui.PopStyleVar(1);
            return changed;
        }

        var names = _names;
        var values = _values;
        for (var i = 0; i < names.Length; i++)
        {
            if (!ImGui.BeginTabItem(names[i])) continue;

            if (Index != i)
            {
                Index = i;
                value = values[i];
                changed = true;
            }

            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();


        ImGui.PopID();
        ImGui.PopStyleVar(1);

        return changed;
    }
}