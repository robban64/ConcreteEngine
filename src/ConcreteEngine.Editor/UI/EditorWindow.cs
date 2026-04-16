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


internal sealed unsafe class EditorWindowMemory
{
    public NativeView<byte> WindowLabelStr = NativeView<byte>.MakeNull();

    public ArenaBlockPtr Memory;
    public NativeView<byte> DataPtr;
    //public RangeU16 NameHandle;
    //public RangeU16 TitleHandle;


    public void Init(string name, string? title, ArenaBlockBuilder memoryBuilder)
    {
        
        WindowLabelStr = Memory.AllocSlice(32 + name.Length + 2);
        var sw = WindowLabelStr.Writer();
        if (!string.IsNullOrEmpty(title))
        {
            sw.Append(title);
        }
        sw.Append("##").Append(name).End();

    }

    public void Commit(ArenaBlockBuilder memoryBuilder)
    {
        Memory = memoryBuilder.Commit();
        DataPtr = Memory.DataPtr;
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
}


internal sealed unsafe class EditorWindow
{
    public string Name;
    public string? Title;

    public bool IsDirty {get; private set;}
    public bool Visible {get; private set;}
    
    public EditorWindowLayout Layout = new();
    public EditorWindowMemory Memory = new();

    private EditorPanel _activePanel = PanelState.EmptyPanel.Instance;
    private PanelSlot _panels = null!;

    private UiDrawCursor _draw;

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
        Memory.Init(Name, Title, memoryBuilder);

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

        Visible = ImGui.Begin(Memory.WindowLabelStr, Layout.Flags);
        if (!Visible)
        {
            ImGui.End();
            return;
        }

        Layout.DrawList = ImGui.GetWindowDrawList();
        _draw = UiDrawCursor.Make(Layout.DrawList);

        var ctx = new WindowContext(ref _draw);
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
            case TransitionAction.Push: PushPanel(msg.Panel, msg.Placement); break;
            case TransitionAction.Pop: PopPanel(msg.Placement); break;
            case TransitionAction.Replace: ReplacePanel(msg.Panel, msg.Placement); break;
            default: throw new ArgumentOutOfRangeException(nameof(msg.Action));
        }
    }

    public void PushPanel(PanelId panelId, PanelPlacement placement)
    {
        _panels.Push(panelId);
        RefreshPanels();
    }

    public void ReplacePanel(PanelId panelId, PanelPlacement placement)
    {
        _panels.Replace(panelId);
        RefreshPanels();
    }

    public void PopPanel(PanelPlacement placement)
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