using System.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal class EnumTabBar<T> where T : unmanaged, Enum
{
    private readonly int _id = Widgets.NextId();
    public int Index;
    public ImGuiTabBarFlags Flags;
    public Vector2 FramePadding = new(12, 4);

    private readonly string[] _names;
    private readonly T[] _values;

    public EnumTabBar(int index = -1, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
    {
        Index = index;
        Flags = flags;

        _names = Enum.GetNames<T>();
        _values = Enum.GetValues<T>();

        if (_names.Length <= 0) throw new ArgumentOutOfRangeException(nameof(_names));
        if (_names.Length != _values.Length) throw new ArgumentOutOfRangeException();
    }

    public EnumTabBar(string[] names, T[] values)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(names.Length, nameof(names));
        ArgumentOutOfRangeException.ThrowIfNotEqual(names.Length, values.Length);
        _names = names;
        _values = values;
    }

    public bool Draw(out T value)
    {
        value = default;
        var sw = Widgets.GetWriter1();
        var names = _names;
        var values = _values;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, FramePadding);
        ImGui.PushID(_id);
        var changed = false;
        if (ImGui.BeginTabBar("##tabs", Flags))
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (!ImGui.BeginTabItem(sw.Write(names[i]))) continue;

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