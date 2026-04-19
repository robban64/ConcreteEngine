using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using static ConcreteEngine.Core.Common.Memory.ArenaAllocator;

namespace ConcreteEngine.Editor.UI;


internal sealed class EditorWindowMemory
{
    public MemoryBlockPtr Memory;
    public RangeU16 NameHandle;
    public RangeU16 TitleHandle;

    public NativeView<byte> NameStr => Memory.DataPtr.Slice(NameHandle);
    public NativeView<byte> TitleStr => Memory.DataPtr.Slice(TitleHandle);

    public void Init(string name, string? title, scoped ref ArenaBlockBuilder memoryBuilder)
    {
        NameHandle = memoryBuilder.AllocStringSlice(name).AsRange16();
        if(!string.IsNullOrEmpty(title))
            TitleHandle = memoryBuilder.AllocStringSlice(title).AsRange16();

        var dataPtr = memoryBuilder.Memory.DataPtr;
        dataPtr.Slice(NameHandle).Writer().Append("##").Append(name).End();
        if (!string.IsNullOrEmpty(title))
            dataPtr.Slice(TitleHandle).Writer().Write(title);
    }

    public void Commit(ArenaBlockBuilder memoryBuilder)
    {
        Memory = memoryBuilder.Commit();
    }
}

internal sealed class EditorWindowLayout
{
    private const ImGuiWindowFlags DefaultFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    public ImDrawListPtr DrawList = ImDrawListPtr.Null;
    public ImGuiWindowFlags Flags = DefaultFlags;
    public Vector2 Position;
    public Vector2 Size;
    public Vector2 SizeMin;
    public Vector2 SizeMax;

    public Action<EditorWindowLayout>? OnRefreshLayout;
}


internal sealed unsafe class EditorWindow
{
    public string Name;
    public string? Title;

    public bool IsDirty {get; private set;}
    public bool Visible {get; private set;}
    
    public readonly EditorWindowLayout Layout = new();
    public readonly EditorWindowMemory Memory = new();

    private EditorPanel _activePanel = PanelState.EmptyPanel.Instance;
    private PanelSlot _panels = null!;

    public EditorWindow(string name, string? title, StateContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, 32);

        Name = name;
        Title = title;
    }

    public void OnCreate(EditorPanel[] panels)
    {
        var memoryBuilder = TextBuffers.PersistentArena.AllocBuilder();
        Memory.Init(Name, Title, ref memoryBuilder);

        _panels = new PanelSlot(panels);
        foreach(var panel in panels)
        {
            //panel.OnCreate(memoryBuilder);
        }

        Memory.Commit(memoryBuilder);
    }

    public void OnEnter()
    {
        _activePanel.OnEnter();
    }
    public void OnLeave()
    {
        _activePanel.OnLeave();
    }

    public void OnUpdateDiagnostic()
    {
        _activePanel.OnUpdateDiagnostic();
    }

    public void OnDraw()
    {
        ImGui.SetNextWindowPos(Layout.Position);
        ImGui.SetNextWindowSize(Layout.Size);
        ImGui.SetNextWindowSizeConstraints(Layout.SizeMin, Layout.SizeMax);

        Visible = ImGui.Begin(Memory.NameStr, Layout.Flags);
        if (!Visible)
        {
            ImGui.End();
            return;
        }

        Layout.DrawList = ImGui.GetWindowDrawList();

       // var drawList = new UiDrawCursor();
       // var ctx = new WindowContext(ref drawList);
        _activePanel.OnDraw(default);

        ImGui.End();
    }

    public void EmitTransition(TransitionMessage msg)
    {
        if (msg.Clear)
        {
            _panels.Clear();
            RefreshPanels();
            return;
        }

        switch (msg.Action)
        {
            case TransitionAction.Push: PushPanel(msg.Panel); break;
            case TransitionAction.Pop: PopPanel(); break;
            case TransitionAction.Replace: ReplacePanel(msg.Panel); break;
            default: throw new ArgumentOutOfRangeException(nameof(msg.Action));
        }
    }

    private void PushPanel(PanelId panelId)
    {
        _panels.Push(panelId);
        RefreshPanels();
    }

    private void ReplacePanel(PanelId panelId)
    {
        _panels.Replace(panelId);
        RefreshPanels();
    }

    private void PopPanel()
    {
        _panels.Pop();
        RefreshPanels();
    }

    private void RefreshPanels()
    {
        var newId = _panels.Current;
        if(newId == _activePanel.Id) return;

        IsDirty = true;
        foreach(var panel in _panels.GetPanels())
        {
            if(panel.Id == newId)
            {
                _activePanel = panel;
                return;
            }
        }

        ConsoleGateway.LogPlain($"Could not find panel {newId} for window {Name}");
    }

}