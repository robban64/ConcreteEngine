using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Assets;
using ConcreteEngine.Editor.UI.Metrics;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelSlot(EditorPanel[] panels)
{
    private readonly List<int> _stack = new(4);

    public PanelId Current => _stack.Count > 0 ? (PanelId)_stack[^1] : PanelId.None;

    public void Clear()
    {
        if (_stack.Count > 0)
        {
            panels[_stack[^1]].OnLeave();
            _stack.Clear();
        }
    }

    public void Push(PanelId id)
    {
        if (_stack.Count > 0)
        {
            var old = _stack[^1];
            if (old == (int)id) return;

            panels[old].OnLeave();
        }

        int index = _stack.IndexOf((int)id);
        if (index >= 0)
            _stack.RemoveAt(index);

        _stack.Add((int)id);
        panels[(int)id].OnEnter();

        if (_stack.Count > 20)
            throw new InvalidOperationException("Check stack growth");
    }

    public void Pop()
    {
        if (_stack.Count == 0)
            return;

        var old = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);

        panels[old].OnLeave();

        if (_stack.Count > 0)
        {
            var newTop = _stack[^1];
            panels[newTop].OnEnter();
        }
    }

    public void Replace(PanelId panel)
    {
        if (_stack.Count > 0)
        {
            var old = _stack[^1];

            if (old == (int)panel)
                return;

            panels[old].OnLeave();
            _stack.RemoveAt(_stack.Count - 1);
        }

        int index = _stack.IndexOf((int)panel);
        if (index >= 0)
            _stack.RemoveAt(index);

        _stack.Add((int)panel);
        panels[(int)panel].OnEnter();
    }
}

internal sealed class PanelState
{
    private readonly EditorPanel[] _panels;
    private readonly PanelSlot _leftSlot;
    private readonly PanelSlot _rightSlot;

    private bool _isDirty;

    public EditorPanel Left { get; private set; }
    public EditorPanel Right { get; private set; }
    public ConsolePanel ConsoleUi { get; private set; }

    public PanelId LeftPanelId => _leftSlot.Current;
    public PanelId RightPanelId => _rightSlot.Current;

    public ReadOnlySpan<EditorPanel> GetPanels() => _panels;

    public PanelState(ConsoleService consoleService)
    {
        _panels = new EditorPanel[11];

        _leftSlot = new PanelSlot(_panels);
        _rightSlot = new PanelSlot(_panels);
        ConsoleUi = new ConsolePanel(consoleService);

        Left = EmptyPanel.Instance;
        Right = EmptyPanel.Instance;
    }

    public void Register(StateContext ctx)
    {
        RegisterPanel(EmptyPanel.Instance);
        RegisterPanel(new MetricsLeftPanel(ctx));
        RegisterPanel(new MetricsRightPanel(ctx));

        RegisterPanel(new AssetListPanel(ctx));
        RegisterPanel(new AssetInspectorPanel(ctx));

        RegisterPanel(new SceneListPanel(ctx));
        RegisterPanel(new SceneInspectorPanel(ctx));

        RegisterPanel(new CameraPanel(ctx));
        RegisterPanel(new AtmospherePanel(ctx));
        RegisterPanel(new LightingPanel(ctx));
        RegisterPanel(new VisualPanel(ctx));

        ConsoleUi.Allocate();
        foreach (var panel in _panels) panel.OnCreate();
    }


    private void RegisterPanel(EditorPanel panel)
    {
        if (_panels[(int)panel.Id] != null) throw new ArgumentException(nameof(panel.Id));
        _panels[(int)panel.Id] = panel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ClearDirty()
    {
        if (!_isDirty) return false;

        _isDirty = false;
        return true;
    }

    /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            Left.Update();
            Right.Update();
        }
    */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateDiagnostic()
    {
        ConsoleUi.OnUpdateDiagnostic();
        Left.OnUpdateDiagnostic();
        Right.OnUpdateDiagnostic();
    }

    public void EmitTransition(TransitionMessage msg)
    {
        if (msg.Clear)
        {
            _leftSlot.Clear();
            _rightSlot.Clear();
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
        if ((uint)panelId >= _panels.Length) throw new ArgumentOutOfRangeException(nameof(panelId));

        switch (placement)
        {
            case PanelPlacement.Left: _leftSlot.Push(panelId); break;
            case PanelPlacement.Right: _rightSlot.Push(panelId); break;
            default: throw new ArgumentOutOfRangeException(nameof(placement));
        }

        RefreshPanels();
    }

    public void ReplacePanel(PanelId panelId, PanelPlacement placement)
    {
        if ((uint)panelId >= _panels.Length) throw new ArgumentOutOfRangeException(nameof(panelId));

        switch (placement)
        {
            case PanelPlacement.Left: _leftSlot.Replace(panelId); break;
            case PanelPlacement.Right: _rightSlot.Replace(panelId); break;
            default: throw new ArgumentOutOfRangeException(nameof(placement));
        }

        RefreshPanels();
    }

    public void PopPanel(PanelPlacement placement)
    {
        switch (placement)
        {
            case PanelPlacement.Left: _leftSlot.Pop(); break;
            case PanelPlacement.Right: _rightSlot.Pop(); break;
            default: throw new ArgumentOutOfRangeException(nameof(placement));
        }

        RefreshPanels();
    }

    private void RefreshPanels()
    {
        _isDirty = true;

        var leftSlot = _leftSlot.Current;
        var rightSlot = _rightSlot.Current;
        Left = _panels[(int)leftSlot];
        Right = _panels[(int)rightSlot];
    }

    private class EmptyPanel : EditorPanel
    {
        public static EmptyPanel Instance = new();

        private EmptyPanel() : base(PanelId.None, null!)
        {
        }

        public override void OnDraw(FrameContext ctx) { }
    }
}