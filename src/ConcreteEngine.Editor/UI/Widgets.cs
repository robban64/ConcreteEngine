using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.UI;

internal static class Widgets
{
    internal ref struct VisibleIterator<T>(int count, int rowHeight, Span<byte> buffer)
    {
        private  Span<byte> _buffer = buffer;

        public static void Run(int count, int rowHeight, T body, Span<byte> buffer, DrawRowDel<T> draw)
        {
            new VisibleIterator<T>(count,rowHeight,buffer).Start(body, draw);
        }

        public void Start(T body, DrawRowDel<T> draw)
        {
            var clipper = new ImGuiListClipper();
            clipper.Begin(count, rowHeight);
            while (clipper.Step())
            {
                int start = clipper.DisplayStart, len = clipper.DisplayEnd;
                for (var i = start; i < len; i++)
                    draw(i, body, ref _buffer);
            }

            clipper.End();
        }
    }
    
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
            if (ImGui.BeginPopup(id)) return true;
            State = false;
            ImGui.PopStyleVar();
            return false;
        }

        public void End()
        {
            ImGui.EndPopup();
            ImGui.PopStyleVar();
        }
    }
}