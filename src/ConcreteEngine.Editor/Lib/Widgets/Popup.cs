using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal struct Popup(Vector2 padding = default)
{
    public Half2 Padding = new(padding);
    public bool State = false;
    private bool _wasOpen;

    public bool Begin(ReadOnlySpan<byte> id, Vector2 position = default)
    {
        if (State && !_wasOpen)
        {
            ImGui.SetNextWindowPos(position, ImGuiCond.Appearing);
            ImGui.OpenPopup(id);
        }

        _wasOpen = State;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Padding);
        if (ImGui.BeginPopup(id))
        {
            return true;
        }

        ImGui.PopStyleVar();
        return State = false;
    }

    public void End()
    {
        ImGui.EndPopup();
        ImGui.PopStyleVar();
    }
}