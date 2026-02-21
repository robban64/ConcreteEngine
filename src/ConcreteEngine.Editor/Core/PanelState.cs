using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels;
using ConcreteEngine.Editor.Panels.Metrics;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelSlot(EditorPanel[] panels)
{
    private readonly List<PanelId> _stack = new(4);

    public PanelId Current => _stack.Count > 0 ? _stack[^1] : PanelId.None;

    public void Clear()
    {
        if (_stack.Count > 0)
        {
            panels[(int)_stack[^1]].Leave();
            _stack.Clear();
        }
    }

    public void Push(PanelId id)
    {
        if (_stack.Count > 0)
        {
            var old = _stack[^1];
            if (old == id) return;

            panels[(int)old].Leave();
        }

        int index = _stack.IndexOf(id);
        if (index >= 0)
            _stack.RemoveAt(index);

        _stack.Add(id);
        panels[(int)id].Enter();

        if (_stack.Count > 20)
            throw new InvalidOperationException("Check stack growth");
    }

    public void Pop()
    {
        if (_stack.Count == 0)
            return;

        var old = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);

        panels[(int)old].Leave();

        if (_stack.Count > 0)
        {
            var newTop = _stack[^1];
            panels[(int)newTop].Enter();
        }
    }

    public void Replace(PanelId panel)
    {
        if (_stack.Count > 0)
        {
            var old = _stack[^1];

            if (old == panel)
                return;

            panels[(int)old].Leave();
            _stack.RemoveAt(_stack.Count - 1);
        }

        int index = _stack.IndexOf(panel);
        if (index >= 0)
            _stack.RemoveAt(index);

        _stack.Add(panel);
        panels[(int)panel].Enter();
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

    public bool ClearDirty()
    {
        if (!_isDirty) return false;

        _isDirty = false;
        return true;
    }

    public PanelState(EngineController controller, PanelContext ctx)
    {
        _panels =
        [
            new EmptyPanel(ctx), new MetricsLeftPanel(ctx), new MetricsRightPanel(ctx),
            new AssetListPanel(ctx, controller.AssetController),
            new AssetPropertyPanel(ctx, controller.AssetController),
            new SceneListPanel(ctx, controller.SceneController), new ScenePropertyPanel(ctx),
            new CameraPanel(ctx), new AtmospherePanel(ctx), new LightingPanel(ctx), new VisualPanel(ctx)
        ];

        for (int i = 0; i < _panels.Length; i++)
        {
            var panel = _panels[i];
            var id = (PanelId)i;
            if (panel is null) throw new InvalidOperationException($"Panel with Id {id.ToString()} is null");
            if (panel.Id != (PanelId)i) throw new InvalidOperationException($"Invalid panel id {panel.Id.ToString()}");
        }

        _leftSlot = new PanelSlot(_panels);
        _rightSlot = new PanelSlot(_panels);

        Left = _panels[0];
        Right = _panels[0];
    }

    public PanelId LeftPanelId => _leftSlot.Current;
    public PanelId RightPanelId => _rightSlot.Current;
    public ReadOnlySpan<EditorPanel> GetPanels() => _panels;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        Left.Update();
        Right.Update();
    }

    public void UpdateDiagnostic()
    {
        Left.UpdateDiagnostic();
        Right.UpdateDiagnostic();
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

    private class EmptyPanel(PanelContext ctx) : EditorPanel(PanelId.None, ctx)
    {
        public override void Draw(in FrameContext ctx) { }
    }
}