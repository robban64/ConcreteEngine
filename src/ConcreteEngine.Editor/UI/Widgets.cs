using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.UI;

internal static class Widgets
{
    public struct Popup(Vector2 padding = default)
    {
        public Vector2 Padding = padding;
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
            if (!ImGui.BeginPopup(id))
            {
                State = false;
                ImGui.PopStyleVar();
                return false;
            }
            
            return true;
        }

        public void End()
        {
            ImGui.EndPopup();
            ImGui.PopStyleVar();
        }
    }
}