using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal struct TextInputFilter
{
    public bool Ascii;
    public bool AlphaNumeric;
}

internal sealed unsafe class TextInput
{
    public ImGuiInputTextFlags InputFlags;

    public readonly ushort BufferSize;

    public bool TrimmedResult, LowercaseAsciiResult;
    public bool AsciiFilter, AlphaNumericFilter;

    private bool _historyActive;
    private short _historyIndex = -1;
    private ushort _historyCapacity;

    private Action<Span<byte>>? _transformCallback;
    private readonly ImGuiInputTextCallback _inputCallback;

    private List<byte[]>? _history;
    private byte[]? _currentInputSnapshot;


    public TextInput(ushort bufferSize, ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 4);
        BufferSize = bufferSize;
        InputFlags = inputFlags;
        _inputCallback = OnInputCallback;
    }

    public TextInput WithHistory(ushort capacity = 32)
    {
        _historyCapacity = capacity;
        _history = new List<byte[]>(capacity);
        _currentInputSnapshot = new byte[BufferSize];

        InputFlags |= ImGuiInputTextFlags.CallbackHistory;
        return this;
    }

    public TextInput WithFilter(bool ascii, bool alphaNumeric)
    {
        AsciiFilter = ascii;
        AlphaNumericFilter = alphaNumeric;
        return this;
    }

    public TextInput WithTransformer(Action<Span<byte>> callback, bool trimmed, bool lowercase)
    {
        _transformCallback = callback;
        TrimmedResult = trimmed;
        LowercaseAsciiResult = lowercase;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Draw(ReadOnlySpan<byte> label, byte* inputStr)
    {
        var triggered = ImGui.InputText(label, inputStr, BufferSize, InputFlags, _inputCallback);
        return triggered && OnTriggered(inputStr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DrawHint(ReadOnlySpan<byte> label, ReadOnlySpan<byte> hint, byte* inputStr)
    {
        var triggered = ImGui.InputTextWithHint(label, hint, inputStr, BufferSize, InputFlags, _inputCallback);
        return triggered && OnTriggered(inputStr);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool OnTriggered(byte* inputStr)
    {
        var src = new Span<byte>(inputStr, BufferSize).SliceNullTerminate();
        if (src.IsEmpty) return false;

        if (AsciiFilter && !UtfText.IsAscii(src)) return false;
        //if(AlphaNumericFilter && !UtfText.)

        if (_transformCallback is { } callback)
        {
            Span<byte> dst = stackalloc byte[src.Length];
            src.CopyTo(dst);
            if (TrimmedResult)
            {
                dst = dst.TrimWhitespace();
                if (dst.IsEmpty) return false;
            }

            if (LowercaseAsciiResult) dst = dst.ToLowerAscii();
            callback(dst);
        }

        if (_history is { } history)
        {
            if (history.Count == 0 || !history[^1].AsSpan().SequenceEqual(src))
            {
                if (history.Count == _historyCapacity) history.RemoveAt(0);
                history.Add(src.ToArray());
            }
        }

        _historyIndex = -1;

        return true;
    }
    
    

    private int OnInputCallback(ImGuiInputTextCallbackData* data)
    {
        var flag = data->EventFlag;
        if (flag == ImGuiInputTextFlags.CallbackEdit)
        {
            _historyActive = false;
            _historyIndex = -1;
        }

        if (flag == ImGuiInputTextFlags.CallbackHistory)
        {
            return OnHistory(data);
        }

        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int OnHistory(ImGuiInputTextCallbackData* data)
    {
        if (_history is null || _currentInputSnapshot is null || _history.Count == 0)
            return 0;

        var key = data->EventKey;

        if (key == ImGuiKey.UpArrow)
        {
            if (!_historyActive)
            {
                SnapshotInput(data, _currentInputSnapshot);
                SetInputBuffer(data, _history[^1]);
                return 0;
            }

            if (_historyIndex < _history.Count - 1) _historyIndex++;

            SetInputBuffer(data, _history[^(_historyIndex + 1)]);
            return 0;
        }

        if (key == ImGuiKey.DownArrow && _historyActive)
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                SetInputBuffer(data, _history[^(_historyIndex + 1)]);
                return 0;
            }

            _historyIndex = -1;
            _historyActive = false;
            SetInputBuffer(data, _currentInputSnapshot.SliceNullTerminate());
        }

        return 0;
    }

    private void SnapshotInput(ImGuiInputTextCallbackData* data, Span<byte> snapshotBuffer)
    {
        snapshotBuffer.Clear();

        var inputBuffer = new Span<byte>(data->Buf, BufferSize);
        inputBuffer.SliceNullTerminate().CopyTo(snapshotBuffer);

        _historyActive = true;
        _historyIndex = 0;
    }

    private static void SetInputBuffer(ImGuiInputTextCallbackData* data, ReadOnlySpan<byte> src)
    {
        var copyLen = int.Min(src.Length, data->BufSize - 1);
        var dst = new Span<byte>(data->Buf, data->BufSize);

        src.CopyTo(dst);
        dst[copyLen] = 0;

        data->BufTextLen = copyLen;
        data->BufDirty = 1;

        data->CursorPos = copyLen;
        data->SelectionStart = data->SelectionEnd = copyLen;
    }
}