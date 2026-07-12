using ConcreteEngine.Core.Common.Collections;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Widgets;

internal sealed unsafe class TextInputHistory
{
    private bool _historyActive;
    private int _historyIndex = -1;
    private readonly int _historyCapacity;

    private readonly List<byte[]> _history;
    private readonly byte[] _currentInputSnapshot;

    public TextInputHistory(int bufferSize, int historyCapacity = 32)
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