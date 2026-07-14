using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Inspection;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

public enum TextInputFilter : byte
{
    None,
    Digit,
    AsciiLetter,
    AsciiLettersAndDigit,
}

internal sealed unsafe class TextInput : InputField
{
    public readonly NativeString Text;
    public String8Utf8 Hint;

    private readonly Delegate? _callback;
    private readonly ImGuiInputTextCallback _inputCallback;

    private TextInputHistory? _history;
    
    public ushort MinLength { get; private set; }
    public bool Trim, Lowercase, ClearAfter, AllowEmpty;
    public TextInputFilter Filter;
    public ImGuiInputTextFlags ImFlags = ImGuiInputTextFlags.CharsNoBlank;
    public string? Whitelist;



    public TextInput(string label, int capacity)
        : base(label, InputKind.Text)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 8);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, 512);
        if (!IntMath.IsPowerOfTwo(capacity)) throw new ArgumentOutOfRangeException(nameof(capacity));

        _inputCallback = OnInputCallback;
        LabelPlacement = LabelPlacement.None;
        Text = StringArena.AllocateString(capacity);
        Text.Reset();
    }

    public TextInput(string label, int capacity, Action<Span<byte>> callback) : this(label, capacity) =>
        _callback = callback;

    public TextInput(string label, int capacity, Action<Span<char>> callback) : this(label, capacity) =>
        _callback = callback;

    public TextInput WithHistory(int capacity = 16)
    {
        if (_history != null) throw new InvalidOperationException();
        _history = new TextInputHistory(Text.Capacity, capacity);
        ImFlags |= ImGuiInputTextFlags.CallbackHistory;
        return this;
    }

    public TextInput WithMinLength(ushort minLength)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minLength, Text.Capacity);
        MinLength = minLength;
        return this;
    }

    public TextInput ToggleFlag(ImGuiInputTextFlags flag, bool enabled)
    {
        if (!enabled) ImFlags &= ~flag;
        else ImFlags |= flag;
        return this;
    }

    public ReadOnlySpan<byte> GetTextSpan() => !Text.IsNull ? Text.AsSpan() : ReadOnlySpan<byte>.Empty;
    
    public bool Draw()
    {
        var hint = Hint;
        var label = ApplyLabelLayout(ScratchBuffer.Writer());
        var triggered = ImGui.InputTextEx(label, (byte*)&hint, Text, Text.Capacity,
            Vector2.Zero, ImFlags, _inputCallback);

        if (triggered && ProcessInput())
        {
            InvokeCallback();
            if (ClearAfter) Text.Clear();
            return true;
        }

        return false;
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

    private void InvokeCallback()
    {
        if (_callback is null) return;

        var text = Text.AsSpan();
        if (_callback is Action<Span<byte>> callbackU8)
        {
            if (text.Length == 0)
            {
                callbackU8(Span<byte>.Empty);
                return;
            }
            Span<byte> dst =  stackalloc byte[text.Length];
            text.CopyTo(dst);
            callbackU8(dst);
        }
        else if (_callback is Action<Span<char>> callbackU16)
        {
            if (text.Length == 0)
            {
                callbackU16(Span<char>.Empty);
                return;
            }
            Span<char> dst = stackalloc char[Encoding.UTF8.GetCharCount(text)];
            Encoding.UTF8.GetChars(text, dst);
           
            callbackU16(dst);
        }
    }

    private bool ProcessInput()
    {
        Text.CalculateLength();
        var src = Text.AsSpan();
        if (src.Length < MinLength || (src.IsEmpty && !AllowEmpty)) return false;

        var hasAsciiFilter = Filter is TextInputFilter.AsciiLetter or TextInputFilter.AsciiLettersAndDigit;
        if (hasAsciiFilter && !UtfText.IsAscii(src)) return false;

        if (Trim)
        {
            src = src.TrimWhitespace();
            if (src.IsEmpty && !AllowEmpty) return false;
        }

        if (Lowercase) src = src.ToLowerAscii();

        if (_history is { } history)
        {
            history.AddEntry(src.ToArray());
            history.LeaveHistoryMode();
        }

        return true;
    }


    private bool FilterChar(char c)
    {
        if (Whitelist is { } w && w.AsSpan().IndexOf(c) >= 0) return true;
        return Filter switch
        {
            TextInputFilter.None => true,
            TextInputFilter.Digit => char.IsAsciiDigit(c),
            TextInputFilter.AsciiLetter => char.IsAsciiLetter(c),
            TextInputFilter.AsciiLettersAndDigit => char.IsAsciiLetterOrDigit(c),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}