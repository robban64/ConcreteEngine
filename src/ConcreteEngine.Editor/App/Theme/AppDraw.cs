using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.App.Shared;
using ConcreteEngine.Editor.Core.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Theme;

internal static unsafe class AppDraw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CollapseSection(ReadOnlySpan<byte> title, delegate*<void> draw, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
    {
        if (ImGui.CollapsingHeader(title, flags)) draw();
        ImGui.Spacing();
        ImGui.Separator();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Section(NativeString title, delegate*<void> draw)
    {
        ImGui.SeparatorText(title);
        draw();
        ImGui.Spacing();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Section(ReadOnlySpan<byte> title, delegate*<void> draw)
    {
        ImGui.SeparatorText(title);
        draw();
        ImGui.Spacing();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Icon(uint icon) => ImGui.TextUnformatted((byte*)&icon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextColored(uint color, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        Text(ScratchBuffer.Write(text));
        ImGui.PopStyleColor();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(string text) => Text(ScratchBuffer.Write(text));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(NativeString text) => ImGui.TextUnformatted(text.TextStart, text.TextEnd);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(NativeView<byte> text) => ImGui.TextUnformatted(text, text + text.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextColumn(NativeView<byte> text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text, text + text.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ColumnV(NativeView<byte> text, float fontSize = GuiTheme.FontSizeDefault)
    {
        ImGui.TableNextColumn();
        var top = ImGui.GetCursorPosY();
        AppLayout.NextAlignTextVertical(top, fontSize);
        ImGui.TextUnformatted(text, text + text.Length);
        return top;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextProperty(ReadOnlySpan<byte> name, NativeView<byte> text)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(text, text + text.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> text)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SameLineProperty(char separator = '-')
    {
        ImGui.SameLine();
        ImGui.TextUnformatted((byte*)&separator);
        ImGui.SameLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Button(byte* text, bool enabled = true)
    {
        if (!enabled) ImGui.BeginDisabled(true);
        var clicked = ImGui.Button(text);
        if (!enabled) ImGui.EndDisabled();
        return enabled && clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ToggleButton(byte* text, bool value, bool enabled = true)
    {
        if (value) ImGui.PushStyleColor(ImGuiCol.Button, Palette32.FrameBgActive);
        var result = Button(text, enabled);
        if (value) ImGui.PopStyleColor();
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Button(uint icon, bool enabled = true) => Button((byte*)&icon, enabled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ToggleButton(uint icon, bool value, bool enabled = true) =>
        ToggleButton((byte*)&icon, value, enabled);


    // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Global
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClipperEnumerator Clipper(int count, float itemHeight, [UnscopedRef] out ImGuiListClipper clipper)
    {
        //out ImGuiListClipper clipper -> allow stack local at call site
        clipper = default;
        clipper.Begin(count, itemHeight);
        return ClipperEnumerator.New(ref clipper);
    }
}