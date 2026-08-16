using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.App.Shared;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Theme;

internal static unsafe class AppDraw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Section(InputGroup inputGroup)
    {
        ImGui.SeparatorText(inputGroup.Label);
        inputGroup.Draw();
        ImGui.Spacing();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CollapseSection(InputGroup inputGroup, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
    {
        if (ImGui.CollapsingHeader(inputGroup.Label, flags)) inputGroup.Draw();
        ImGui.Spacing();
        ImGui.Separator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CollapseSection(ReadOnlySpan<byte> title, delegate*<void> draw,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
    {
        if (ImGui.CollapsingHeader(title, flags)) draw();
        ImGui.Spacing();
        ImGui.Separator();
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
    public static void TextColored(uint color, ReadOnlySpan<char> text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        Text(ScratchBuffer.Write(text));
        ImGui.PopStyleColor();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextColored(uint color, ReadOnlySpan<byte> text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(string text) => Text(ScratchBuffer.Write(text));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(NativeString text) => ImGui.TextUnformatted(text.TextStart, text.TextEnd);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(NativeView<byte> text) => ImGui.TextUnformatted(text, text.EndPtr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextColumn(NativeView<byte> text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text, text.EndPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ColumnV(NativeView<byte> text, float fontSize = GuiTheme.FontSizeDefault)
    {
        ImGui.TableNextColumn();
        var top = ImGui.GetCursorPosY();
        AppLayout.NextAlignTextVertical(top, fontSize);
        ImGui.TextUnformatted(text, text.EndPtr);
        return top;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextProperty(ReadOnlySpan<byte> name, NativeView<byte> text)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(text, text.EndPtr);
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
    public static bool Button(uint icon, bool enabled = true) => Button((byte*)&icon, enabled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ToggleButton(byte* text, bool value, bool enabled = true)
    {
        if (value) ImGui.PushStyleColor(ImGuiCol.Button, Palette32.FrameBgActive);
        if (!enabled) ImGui.BeginDisabled(true);
        var clicked = ImGui.Button(text);
        if (!enabled) ImGui.EndDisabled();
        if (value) ImGui.PopStyleColor();
        return enabled && clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ToggleButton(uint icon, bool value, bool enabled = true) =>
        ToggleButton((byte*)&icon, value, enabled);

    public static bool ImageButton(ReadOnlySpan<byte> strId, TextureId id, ref TexturePtrHandle handle, Vector2 size)
    {
        if (ImGuiSystem.TryResolveTextureHandle(id, ref handle))
            return ImGui.ImageButton(strId, handle, size);

        return ImGui.Button(strId, size);
    }


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