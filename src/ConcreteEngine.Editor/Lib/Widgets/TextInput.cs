using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

public enum TextInputFilter : byte
{
    None,
    Digit,
    AsciiLetter,
    AsciiLettersAndDigit,
}
internal sealed unsafe class TextInput : UiField
{
    private byte* _textBuffer = null;

    public String8Utf8 Hint;

    public readonly ushort BufferSize;
    public ushort MinLength;

    public bool ClearOnResult;
    public bool AllowEmptyResult;

    public ImGuiInputTextFlags InputFlags;
    public TextInputFilter InputFilter;
    private char[] _whiteListFilter = [];

    private TextInputHistory? _history;
    private TextInputTransformer? _transformer;
    private readonly ImGuiInputTextCallback _inputCallback;

    public TextInput(string label, ushort bufferSize, ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank)
        : base(label, FieldWidgetKind.InputText)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 4);
        BufferSize = bufferSize;
        InputFlags = inputFlags;
        _inputCallback = OnInputCallback;

        Layout = FieldLayout.None;
    }

    public override ref byte GetRawValue()
    {
        if (_textBuffer == null) throw new NullReferenceException(nameof(_textBuffer));
        return ref _textBuffer[0];
    }

    public void UnsetTextBuffer() => _textBuffer = null;

    public void SetTextBuffer(NativeView<byte> buffer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(BufferSize, buffer.Length);
        _textBuffer = buffer;
        buffer.Clear();
    }

    [SkipLocalsInit]
    public override bool Draw()
    {
        var buffer = stackalloc byte[LabelAllocCapacity];
        var label = ApplyLabelLayout(buffer);

        var hint = Hint;
        var size = new Vector2(Width, 0);
        var triggered = ImGui.InputTextEx(label, (byte*)&hint, _textBuffer, BufferSize, size, InputFlags, _inputCallback);

        return triggered && OnTriggered(_textBuffer);
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
        if (src.Length < MinLength) return false;
        if (src.IsEmpty && !AllowEmptyResult) return false;

        var hasAsciiFilter = InputFilter is TextInputFilter.AsciiLetter or TextInputFilter.AsciiLettersAndDigit;
        if (hasAsciiFilter && !UtfText.IsAscii(src)) return false;

        if (_transformer is { } transformer && !transformer.Transform(src, AllowEmptyResult))
            return false;

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
        if (_whiteListFilter.Length > 0 && _whiteListFilter.IndexOf(c) >= 0)
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


    public TextInput WithHistory(ushort capacity = 32)
    {
        _history = new TextInputHistory(BufferSize, capacity);
        InputFlags |= ImGuiInputTextFlags.CallbackHistory;
        return this;
    }

    public TextInput WithMinLength(ushort minLength)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minLength, BufferSize);
        MinLength = minLength;
        return this;
    }

    public TextInput WithClearOnResult()
    {
        ClearOnResult = true;
        return this;
    }

    public TextInput WithFilter(TextInputFilter filter, bool allowEmpty = false, char[]? whiteListFilter = null)
    {
        InputFilter = filter;
        if (whiteListFilter != null) _whiteListFilter = whiteListFilter;
        return this;
    }

    public TextInput WithTransformer(bool trimmed, bool lowercase = false)
    {
        _transformer ??= new TextInputTransformer();
        _transformer.TrimmedResult = trimmed;
        _transformer.LowercaseResult = lowercase;
        return this;
    }

    public TextInput WithCallbackU8(Action<Span<byte>> callback)
    {
        _transformer ??= new TextInputTransformer();
        _transformer.CallbackU8 = callback;
        return this;
    }

    public TextInput WithCallbackU16(Action<Span<char>> callback)
    {
        _transformer ??= new TextInputTransformer();
        _transformer.CallbackU16 = callback;
        return this;
    }


    private sealed unsafe class TextInputHistory
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

    private sealed class TextInputTransformer
    {
        public bool TrimmedResult;
        public bool LowercaseResult;

        public Action<Span<byte>>? CallbackU8;
        public Action<Span<char>>? CallbackU16;

        public bool Transform(Span<byte> src, bool allowEmpty)
        {
            if (TrimmedResult)
            {
                src = src.TrimWhitespace();
                if (src.IsEmpty && !allowEmpty) return false;
            }

            if (LowercaseResult) src = src.ToLowerAscii();

            if (CallbackU8 is { } callbackU8)
            {
                Span<byte> dst = stackalloc byte[src.Length];
                src.CopyTo(dst);
                callbackU8(dst);
            }
            else if (CallbackU16 is { } callbackU16)
            {
                Span<char> dst = stackalloc char[Encoding.UTF8.GetCharCount(src)];
                Encoding.UTF8.GetChars(src, dst);
                callbackU16(dst);
            }
            return true;
        }
    }


}

