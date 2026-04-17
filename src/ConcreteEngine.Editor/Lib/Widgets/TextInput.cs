using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

public enum TextInputFilter : byte
{
    None,
    Digit,
    AsciiLetter,
    AsciiLettersAndDigit,
}

internal sealed unsafe class TextInputHistory
{
    private bool _historyActive;
    private short _historyIndex = -1;
    private readonly ushort _historyCapacity;
    
    private readonly List<byte[]> _history;
    private readonly byte[] _currentInputSnapshot;

    public TextInputHistory(ushort bufferSize, ushort historyCapacity = 32)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 4);
        ArgumentOutOfRangeException.ThrowIfLessThan(historyCapacity, 2);

        _currentInputSnapshot = new byte[bufferSize];
        _historyCapacity = historyCapacity;
        _history = new List<byte[]>(_historyCapacity);
    }

    public void LeaveHistoryMode()
    {
        _historyActive = false;
        _historyIndex = -1;
    }

    public void AddEntry(Span<byte> src)
    {
        if (_history.Count != 0 && _history[^1].AsSpan().SequenceEqual(src))
            return;

        if (_history.Count == _historyCapacity) _history.RemoveAt(0);
        _history.Add(src.ToArray());
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int OnInputCallback(ImGuiInputTextCallbackData* data)
    {
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

        var inputBuffer = new Span<byte>(data->Buf, _currentInputSnapshot.Length);
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

internal sealed unsafe class TextInput
{
    public readonly ushort BufferSize;
    
    public char[] WhiteListFilter = [];

    public ImGuiInputTextFlags InputFlags;

    public bool ClearOnResult;
    public bool TrimmedResult, LowercaseResult;
    public bool AllowEmptyResult;
    
    public TextInputFilter InputFilter;

    private TextInputHistory? _history;
    
    private Action<Span<byte>>? _callbackU8;
    private Action<Span<char>>? _callbackU16;

    private readonly ImGuiInputTextCallback _inputCallback;

    public TextInput(ushort bufferSize,
        ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsNoBlank)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 4);
        BufferSize = bufferSize;
        InputFlags = inputFlags;
        _inputCallback = OnInputCallback;
    }

    public TextInput WithHistory(ushort capacity = 32)
    {
        _history = new TextInputHistory(BufferSize, capacity);
        InputFlags |= ImGuiInputTextFlags.CallbackHistory;
        return this;
    }

    public TextInput WithClearOnResult()
    {
        ClearOnResult = true;
        return this;
    }

    public TextInput WithFilter(TextInputFilter filter, char[]? whiteListFilter = null)
    {
        InputFilter = filter;
        if (whiteListFilter != null) WhiteListFilter = whiteListFilter;
        return this;
    }

    public TextInput WithTransformer(bool trimmed, bool lowercase = false, bool allowEmpty = false)
    {
        TrimmedResult = trimmed;
        LowercaseResult = lowercase;
        AllowEmptyResult = allowEmpty;
        return this;
    }


    public TextInput WithCallbackU8(Action<Span<byte>> callback)
    {
        _callbackU8 = callback;
        return this;
    }

    public TextInput WithCallbackU16(Action<Span<char>> callback)
    {
        _callbackU16 = callback;
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

    private int OnInputCallback(ImGuiInputTextCallbackData* data)
    {
        var flag = data->EventFlag;
        if (flag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            var c = (char)data->EventChar;
            if (!FilterChar(c)) return 0;
        }

        if (_history is { } history)
        {
            if (flag == ImGuiInputTextFlags.CallbackEdit)
                history.LeaveHistoryMode();

            else if (flag == ImGuiInputTextFlags.CallbackHistory)
                return history.OnInputCallback(data);
        }

        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool OnTriggered(byte* inputStr)
    {
        var src = new Span<byte>(inputStr, BufferSize).SliceNullTerminate();
        if (src.IsEmpty && !AllowEmptyResult) return false;

        var hasAsciiFilter = InputFilter is TextInputFilter.AsciiLetter or TextInputFilter.AsciiLettersAndDigit;
        if (hasAsciiFilter && !UtfText.IsAscii(src)) return false;

        if (TrimmedResult)
        {
            src = src.TrimWhitespace();
            if (src.IsEmpty && !AllowEmptyResult) return false;
        }

        if (LowercaseResult) src = src.ToLowerAscii();

        if (_callbackU8 is { } callbackU8)
        {
            Span<byte> dst = stackalloc byte[src.Length];
            src.CopyTo(dst);
            callbackU8(dst);
        }
        else if (_callbackU16 is { } callbackU16)
        {
            Span<char> dst = stackalloc char[Encoding.UTF8.GetCharCount(src)];
            Encoding.UTF8.GetChars(src, dst);
            callbackU16(dst);
        }

        if (_history is { } history)
        {
            history.AddEntry(src.ToArray());
            history.LeaveHistoryMode();
        }

        if (ClearOnResult) src.Clear();
        return true;
    }

    private bool FilterChar(char c)
    {
        if (WhiteListFilter.Length > 0 && WhiteListFilter.IndexOf(c) >= 0)
            return true;

        return InputFilter switch
        {
            TextInputFilter.None => true,
            TextInputFilter.Digit => char.IsAsciiDigit(c),
            TextInputFilter.AsciiLetter => char.IsAsciiLetter(c),
            TextInputFilter.AsciiLettersAndDigit => char.IsAsciiLetterOrDigit(c),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}