using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.UI;
/*
internal ref struct BufferDraw(Span<byte> buffer)
{
    private ZaUtf8SpanWriter _za = ZaUtf8SpanWriter.Create(buffer);
    public void Write(ReadOnlySpan<byte> text) => _za.Append(text);
    public void Write(int value) => _za.Append(value);

    public readonly ref struct Writer(ref BufferDraw draw)
    {
        private readonly BufferDraw _draw = draw;
        public ZaUtf8SpanWriter Za => _draw._za;
        public static implicit operator ZaUtf8SpanWriter(Writer w) => w.Za;
    }
}

internal static class BufferDrawExtensions
{
    extension(ref BufferDraw draw)
    {
        public BufferDraw.Writer Write(ref ZaUtf8SpanWriter za) => new BufferDraw.Writer(ref draw);

        public ref BufferDraw TextUnformatted(ref ZaUtf8SpanWriter za)
        {
            ImGui.TextUnformatted(za.AsSpan());
            za.Clear();
            return ref draw;
        }

        public ref BufferDraw SeparatorTextId(ReadOnlySpan<byte> prop, int id, ref ZaUtf8SpanWriter za)
        {
            ImGui.SeparatorText(za.Append(prop).Append(" ["u8).Append(id).AppendEnd("]"u8).AsSpan());
            za.Clear();
            return ref draw;
        }


        public ref BufferDraw NextRowPushId(ref ZaUtf8SpanWriter za, ImGuiTableRowFlags flags = ImGuiTableRowFlags.None,
            float rowHeight = 0)
        {
            ImGui.PushID(za.AsSpan());
            ImGui.TableNextRow(flags, rowHeight);
            za.Clear();
            return ref draw;
        }

        public ref BufferDraw DrawColumn(ref ZaUtf8SpanWriter za)
        {
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(za.AsSpan());
            za.Clear();
        }

        public ref BufferDraw DrawRightProp(ReadOnlySpan<byte> t1, ref ZaUtf8SpanWriter za)
        {
            ImGui.TextUnformatted(t1);
            ImGui.SameLine();
            ImGui.TextUnformatted(za.AsSpan());
            za.Clear();
            return ref draw;
        }

        public ref BufferDraw DrawRightPropColor(in Color4 color, ReadOnlySpan<byte> t1, ref ZaUtf8SpanWriter za)
        {
            ImGui.TextUnformatted(t1);
            ImGui.SameLine();
            ImGui.TextColored(color, za.AsSpan());
            za.Clear();
            return ref draw;
        }
    }
}*/