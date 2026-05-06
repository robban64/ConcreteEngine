using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed class EditorWindowLayout
{
    public Vector2 Position;
    public Vector2 Size;
    public Vector2 SizeMin;
    public Vector2 SizeMax;
    public Vector2? WindowPadding;
    public uint? BgColor;

    public bool NoBorder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyStyle()
    {
        ImGui.SetNextWindowPos(Position);
        ImGui.SetNextWindowSize(Size);
        if(SizeMax != default)
            ImGui.SetNextWindowSizeConstraints(SizeMin, SizeMax);

        if (WindowPadding is { } windowPadding) 
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, windowPadding);

        if(NoBorder) 
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

        if (BgColor is { } bgColor)
            ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndStyle()
    {
        if (WindowPadding.HasValue) ImGui.PopStyleVar();
        if(NoBorder) ImGui.PopStyleVar();

        if (BgColor.HasValue) ImGui.PopStyleColor();

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
    public EditorPanel? PendingPanel { get; private set; }
    public EditorPanel? ActivePanel {get; private set;}

    public readonly EditorWindowLayout Layout;

    public MemoryBlockPtr Memory;
    private RangeU16 _labelHandle;
    
    //private readonly Stack<EditorPanel> _backStack = new();
    //public Action<StateManager>? CustomDrawer;

    public EditorWindow(string name, WindowId id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        Id = id;
        Layout = new EditorWindowLayout();
    }

    public void OnDraw()
    {
        if (PendingPanel is not null)
            ApplyPanel();

        Layout.ApplyStyle();

        Visible = ImGui.Begin(Memory.Data.Slice(_labelHandle), Flags);
        if (Visible && ActivePanel is {} activePanel)
        {
            //CustomDrawer?.Invoke(_stateManager);
            activePanel.OnDraw();
        }
        ImGui.End();
        Layout.EndStyle();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        Memory.ResetCursor();

        _labelHandle = Memory.AllocStringSlice(Name).AsRange16();

        ActivePanel = PendingPanel;
        ActivePanel.DataPtr = Memory.Data;
        ActivePanel.OnEnter(ref Memory);

        PendingPanel = null;
    }

}