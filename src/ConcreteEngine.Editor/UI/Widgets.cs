using System.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class Widgets
{
    public struct Popup(int id, Vector2 padding = default)
    {
        public readonly Vector2 Padding = padding;
        public readonly byte Id = (byte)id;
        public bool State = false;

        public void Open() => State = true;
        public void Close() => State = false;
        public void Toggle() => State = !State;

        public PopupScope Begin(Vector2 position = default)
        {
            if (!State) return new PopupScope(false);

            var id = new ReadOnlySpan<byte>(in Id);
            if (!ImGui.IsPopupOpen(id))
            {
                ImGui.SetNextWindowPos(position);
                ImGui.OpenPopup(Id);
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Padding);


            if (ImGui.BeginPopup(id)) return new PopupScope(true);

            State = false;
            ImGui.PopStyleVar();
            return new PopupScope(false);

        }

        public readonly ref struct PopupScope(bool visible)
        {
            public void Dispose()
            {
                if (!visible) return;
                ImGui.EndPopup();
                ImGui.PopStyleVar();
            }
        }
    }
}