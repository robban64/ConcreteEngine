using System.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.UI;

internal struct Popup
{
    public bool State;
    private bool _wasOpen;

    public bool Begin(ReadOnlySpan<byte> id, Vector2 position = default)
    {
        if (State && !_wasOpen)
        {
            ImGui.SetNextWindowPos(position, ImGuiCond.Appearing);
            ImGui.OpenPopup(id);
        }

        _wasOpen = State;

        if (ImGui.BeginPopup(id)) return true;

        return State = false;
    }

    public void Close()
    {
        State = false;
        ImGui.CloseCurrentPopup();
    }

    public readonly void End()
    {
        ImGui.EndPopup();
    }
}