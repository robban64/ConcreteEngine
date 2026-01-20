using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Panels;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelSlot(EditorPanel?[] panels)
{
    private readonly Stack<PanelId> _stack = new(16);

    public PanelId Current => _stack.TryPeek(out var it) ? it : PanelId.None;

    public void Clear() => _stack.Clear();

    public void Push(PanelId id)
    {
        if (_stack.TryPeek(out var old))
        {
            if (old == id) return;
            panels[(int)old]?.Leave();
        }

        _stack.Push(id);
        panels[(int)id]?.Enter();
        if (_stack.Count > 20) throw new InvalidOperationException("Check stack growth");
    }

    public void Pop()
    {
        if (_stack.Count == 0) return;

        panels[(int)_stack.Pop()]?.Leave();
        if (_stack.TryPeek(out var current)) panels[(int)current]?.Enter();
    }

    public void Replace(PanelId panel)
    {
        if (_stack.Count > 0)
        {
            if (_stack.Peek() == panel) return;
            panels[(int)_stack.Pop()]?.Leave();
        }

        _stack.Push(panel);
        panels[(int)panel]?.Enter();
    }
}

internal sealed class PanelState
{
    private readonly EditorPanel?[] _panels;
    private readonly PanelSlot _leftSlot;
    private readonly PanelSlot _rightSlot;

    public EditorPanel? Left { get; private set; }
    public EditorPanel? Right { get; private set; }

    public PanelState()
    {
        _panels =
        [
            null!, new MetricsLeftPanel(), new MetricsRightPanel(),
            new AssetListPanel(), new AssetPropertyPanel(),
            new SceneListPanel(), new ScenePropertyPanel(),
            new WorldPanel(), new VisualPanel()
        ];

        for (int i = 1; i < _panels.Length; i++)
        {
            var panel = _panels[i];
            var id = (PanelId)i;
            if (panel is null) throw new InvalidOperationException($"Panel with Id {id.ToString()} is null");
            if (panel.Id != (PanelId)i) throw new InvalidOperationException($"Invalid panel id {panel.Id.ToString()}");
        }

        _leftSlot = new PanelSlot(_panels);
        _rightSlot = new PanelSlot(_panels);
    }

    public PanelId LeftPanelId => _leftSlot.Current;
    public PanelId RightPanelId => _rightSlot.Current;
    public ReadOnlySpan<EditorPanel?> GetPanels() => _panels;

    public void Update()
    {
        Left?.Update();
        Right?.Update();
    }

    public void UpdateDiagnostic()
    {
        Left?.UpdateDiagnostic();
        Right?.UpdateDiagnostic();
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
        var leftSlot = _leftSlot.Current;
        var rightSlot = _rightSlot.Current;
        Left = leftSlot > 0 ? _panels[(int)leftSlot] : null;
        Right = rightSlot > 0 ? _panels[(int)rightSlot] : null;
    }
}