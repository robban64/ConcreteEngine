using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.UI;

internal sealed class DrawContext
{
    private readonly byte[] _buffer = new byte[128];

    public static readonly DrawContext Instance = new();

    public FrameContext GetCtx(float dt, ModeState mode) => new(_buffer.AsSpan(), dt, mode);

    public ZaUtf8SpanWriter GetWriter() => ZaUtf8SpanWriter.Create(_buffer.AsSpan());

    public void SeparatorTextId(ReadOnlySpan<byte> prop, int id)
    {
        var write = GetWriter();
        ImGui.SeparatorText(write.Append(prop).Append(" ["u8).Append(id).AppendEnd("]"u8).AsSpan());
        write.Clear();
    }


    public void NextColumn(ReadOnlySpan<byte> text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text);
    }

    public void NextColumnColor(ReadOnlySpan<byte> text, in Color4 c)
    {
        ImGui.TableNextColumn();
        ImGui.TextColored(c, text);
    }

    public void DrawRightProp(ReadOnlySpan<byte> t1, ReadOnlySpan<byte> t2)
    {
        ImGui.TextUnformatted(t1);
        ImGui.SameLine();
        ImGui.TextUnformatted(t2);
    }

    public void DrawRightPropColor(in Color4 color, ReadOnlySpan<byte> t1, ReadOnlySpan<byte> t2)
    {
        ImGui.TextUnformatted(t1);
        ImGui.SameLine();
        ImGui.TextColored(color, t2);
    }

    public void DrawTooltip(ReadOnlySpan<byte> text, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (!ImGui.IsItemHovered(flags)) return;
        if (ImGui.BeginTooltip())
        {
            ImGui.TextUnformatted(text);
            ImGui.EndTooltip();
        }
    }
}