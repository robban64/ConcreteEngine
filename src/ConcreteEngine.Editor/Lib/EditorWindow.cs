using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed unsafe class EditorWindow
{
    private const ImGuiWindowFlags DefaultFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    public readonly string Name;
    public readonly WindowId Id;

    public ImGuiWindowFlags Flags = DefaultFlags;

    public uint? BgColor;
    public bool NoBorder;

    public bool IsDirty { get; private set; }
    public bool Visible { get; private set; }
    public EditorPanel? PendingPanel { get; private set; }
    public EditorPanel? ActivePanel { get; private set; }

    public MemoryBlockPtr Memory;
    private RangeU16 _labelHandle;

    //private readonly Stack<EditorPanel> _backStack = new();
    //public Action<StateManager>? CustomDrawer;

    public EditorWindow(string name, WindowId id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        Id = id;
    }

    public void OnDraw()
    {
        if (PendingPanel is not null)
            ApplyPanel();

        if (NoBorder)
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        if (BgColor is { } bgColor)
            ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);

        Visible = ImGui.Begin(Memory.Data.Slice(_labelHandle), Flags);
        if (Visible && ActivePanel is { } activePanel)
        {
            //CustomDrawer?.Invoke(_stateManager);
            activePanel.OnDraw();
        }

        ImGui.End();


        if (NoBorder) ImGui.PopStyleVar();
        if (BgColor.HasValue) ImGui.PopStyleColor();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnUpdateDiagnostic() => ActivePanel?.OnUpdateDiagnostic();

    public void EnqueuePanel(EditorPanel panel) => PendingPanel = panel;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ApplyPanel()
    {
        if (PendingPanel is null) return;

        if (ActivePanel is not null)
        {
            ActivePanel.OnLeave();
            ActivePanel.DataPtr = NativeView<byte>.MakeNull();
        }

        Memory.ResetCursor();

        var allocator = Memory.GetAllocator();
        _labelHandle = allocator.AllocStringSlice(Name).AsRange16();

        ActivePanel = PendingPanel;
        ActivePanel.DataPtr = Memory.Data;
        ActivePanel.OnEnter(allocator);

        PendingPanel = null;
    }
}