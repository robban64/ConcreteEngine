using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;


internal class EnumTabBar<T>(int index = -1, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
    where T : unmanaged, Enum
{
    
    private readonly int _id = Widgets.NextId();

    public int Index = index;
    public Vector2 FramePadding = new(12, 4);
    public readonly ImGuiTabBarFlags Flags = flags;

    public bool Draw(out T value)
    {
        value = default;

        var names = EnumCache<T>.GetNames();
        var values = EnumCache<T>.GetValues();
        var sw = Widgets.GetWriter1();

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, FramePadding);
        ImGui.PushID(_id);
        var changed = false;
        if (ImGui.BeginTabBar("##tabs"))
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