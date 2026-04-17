using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class TextInput
{
    public ImGuiInputTextFlags InputFlags;

    public readonly ushort BufferSize;
    public bool EmptyResult, TrimmedResult, AsciiResult; // used in ResultCallback

    public Action<Span<byte>>? ResultCallback;
    public ImGuiInputTextCallback? InputCallback;

    public TextInput(ushort bufferSize, ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 4);
        BufferSize = bufferSize;
        InputFlags = inputFlags;
    }

    public TextInput WithInputCallback(ImGuiInputTextCallback callback, ImGuiInputTextFlags callbackFlags)
    {
        InputCallback = callback;
        InputFlags |= callbackFlags;
        return this;
    }

    public TextInput WithResultCallback(Action<Span<byte>> callback, bool empty = true, bool trim = true,
        bool ascii = true)
    {
        ResultCallback = callback;
        EmptyResult = empty;
        TrimmedResult = trim;
        AsciiResult = ascii;
        return this;
    }


    public bool Draw(ReadOnlySpan<byte> label, byte* inputStr)
    {
        var triggered = ImGui.InputText(label, inputStr, BufferSize, InputFlags, InputCallback);
        if (triggered && ResultCallback is { } textCallback)
            return OnTriggered(inputStr, textCallback);

        return triggered;
    }

    public bool DrawHint(ReadOnlySpan<byte> label, ReadOnlySpan<byte> hint, byte* inputStr)
    {
        var triggered = ImGui.InputTextWithHint(label, hint, inputStr, BufferSize, InputFlags, InputCallback);
        if (triggered && ResultCallback is { } textCallback)
            return OnTriggered(inputStr, textCallback);

        return triggered;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool OnTriggered(byte* inputStr, Action<Span<byte>> callback)
    {
        var src = new Span<byte>(inputStr, BufferSize).SliceNullTerminate();
        if (src.IsEmpty && !EmptyResult) return false;

        if (AsciiResult && !UtfText.IsAscii(src)) return false;

        Span<byte> dst = stackalloc byte[src.Length];
        src.CopyTo(dst);

        if (TrimmedResult)
        {
            dst = dst.TrimWhitespace();
            if (dst.IsEmpty && !EmptyResult) return EmptyResult;
        }

        callback(dst);

        return true;
    }
}