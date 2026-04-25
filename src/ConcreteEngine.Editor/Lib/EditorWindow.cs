using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;
/*
internal sealed class EditorWindowMemory : IDisposable
{
    private NativeArray<byte> _buffer;
    public NativeView<byte> PanelData;
    private readonly RangeU16 _labelHandle;

    private int _cursor;

    public NativeView<byte> LabelStr => _buffer.Slice(_labelHandle);

    public EditorWindowMemory(string name, int initalCapacity)
    {
        _buffer = NativeArray.Allocate<byte>(initalCapacity);

        var sw = new UnsafeSpanWriter(_buffer);
        _labelHandle = sw.Write(name).AsRange16();

        var offset = _labelHandle.Length;
        PanelData = _buffer.Slice(offset, _buffer.Length - offset);

    }


    public void ResetCursor()
    {
        _cursor = 0;
    }

    public NativeView<byte> AllocSlice(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        length = IntMath.AlignUp(length, 4);
        if ((uint)_cursor + (uint)length > (uint)PanelData.Length)
            throw new InsufficientMemoryException(length.ToString());

        var start = _cursor;
        _cursor += length;
        return PanelData.Slice(start, length);
    }

    public void Dispose()
    {
        _buffer.Dispose();
        PanelData = NativeView<byte>.MakeNull();
    }
}
*/

internal sealed class EditorWindowLayout
{
    public Vector2 Position;
    public Vector2 Size;
    public Vector2 SizeMin;
    public Vector2 SizeMax;
    public Vector2? WindowPadding;
    public uint? BgColor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyStyle()
    {
        ImGui.SetNextWindowPos(Position);
        ImGui.SetNextWindowSize(Size);
        if(SizeMax != default)
            ImGui.SetNextWindowSizeConstraints(SizeMin, SizeMax);

        if (WindowPadding is { } windowPadding)
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, windowPadding);

        if (BgColor is { } bgColor)
            ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndStyle()
    {
        if (WindowPadding.HasValue)
            ImGui.PopStyleVar();

        if (BgColor.HasValue)
            ImGui.PopStyleColor();
    }
}


internal sealed unsafe class EditorWindow
{
    private const ImGuiWindowFlags DefaultFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    public readonly string Name;
    public readonly WindowId Id;
    public ImGuiWindowFlags Flags = DefaultFlags;

    public bool IsDirty { get; private set; }
    public bool Visible { get; private set; }
    public EditorPanel? PendingPanel { get; private set; } = null;
    public EditorPanel? ActivePanel {get; private set;} = null;
    //private readonly Stack<EditorPanel> _backStack = new();

    public readonly EditorWindowLayout Layout;
    
    private MemoryBlockPtr _memory;
    private RangeU16 _labelHandle;

    public EditorWindow(string name, WindowId id, StateManager state, int allocatorCapacity = 128)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfLessThan(allocatorCapacity, 128);

        Name = name;
        Id = id;
        Layout = new EditorWindowLayout();
        
        _memory = id switch
        {
            WindowId.Left => TextBuffers.WindowMemory1,
            WindowId.Right => TextBuffers.WindowMemory2,
            WindowId.Bottom => TextBuffers.WindowMemory3,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    [SkipLocalsInit]
    public void OnDraw()
    {
        if (PendingPanel is not null)
            ApplyPanel();

        Layout.ApplyStyle();

        Visible = ImGui.Begin(_memory.DataPtr.Slice(_labelHandle), Flags);
        if (Visible)
        {
            var buffer = stackalloc byte[128];
            ActivePanel?.OnDraw(new FrameContext(buffer, 128));
        }
        ImGui.End();
        Layout.EndStyle();
    }

    public void OnUpdateDiagnostic() => ActivePanel?.OnUpdateDiagnostic();

    public void EnqueuePanel(EditorPanel panel) => PendingPanel = panel;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ApplyPanel()
    {
        if(PendingPanel is null) return;

        if (ActivePanel is not null)
        {
            ActivePanel.OnLeave();
            ActivePanel.DataPtr = NativeView<byte>.MakeNull();
        }
        _memory.ResetCursor();

        var nameView = _memory.AllocSlice(Encoding.UTF8.GetByteCount(Name));
        nameView.Writer().Write(Name);
        _labelHandle = nameView.AsRange16();

        ActivePanel = PendingPanel;
        ActivePanel.DataPtr = _memory.DataPtr;
        ActivePanel.OnEnter(ref _memory);

        PendingPanel = null;
    }

}