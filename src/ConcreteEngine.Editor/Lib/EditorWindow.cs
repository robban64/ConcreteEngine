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

    public ImGuiWindowFlags Flags = DefaultFlags;

    public bool NoBorder;

    public bool Visible { get; private set; }
    public EditorPanel? PendingPanel { get; private set; }
    public EditorPanel? ActivePanel { get; private set; }

    public MemoryBlockPtr Memory;
    private RangeU16 _labelHandle;

    public EditorWindow(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    public void OnDraw()
    {
        if (PendingPanel is not null)
            ApplyPanel();

        if (NoBorder)
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

        Visible = ImGui.Begin(Memory.Data.Slice(_labelHandle), Flags);
        if (Visible && ActivePanel is { } activePanel)
        {
            //CustomDrawer?.Invoke(_stateManager);
            activePanel.OnDraw();
        }

        ImGui.End();


        if (NoBorder) ImGui.PopStyleVar();
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