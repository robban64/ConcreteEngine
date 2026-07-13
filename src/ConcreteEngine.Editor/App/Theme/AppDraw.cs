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
    public static void Icon(uint icon) => ImGui.TextUnformatted((byte*)&icon);

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
    public static void DrawTextProperty(ReadOnlySpan<byte> name, NativeView<byte> text)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(text, text + text.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> text)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawSameLineProperty(char separator = '-')
    {
        ImGui.SameLine();
        ImGui.TextUnformatted((byte*)&separator);
        ImGui.SameLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawButton(byte* text, bool enabled = true)
    {
        if (!enabled) ImGui.BeginDisabled(true);
        var clicked = ImGui.Button(text);
        if (!enabled) ImGui.EndDisabled();
        return enabled && clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawToggleButton(byte* text, bool value, bool enabled = true)
    {
        if (value) ImGui.PushStyleColor(ImGuiCol.Button, Palette32.FrameBgActive);
        var result = DrawButton(text, enabled);
        if (value) ImGui.PopStyleColor();
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawButton(uint icon, bool enabled = true) => DrawButton((byte*)&icon, enabled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawToggleButton(uint icon, bool value, bool enabled = true) =>
        DrawToggleButton((byte*)&icon, value, enabled);

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