using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelSlot
{
    private readonly Stack<ComponentRuntime> _stack = new(16);

    public ComponentRuntime? Current => _stack.TryPeek(out var it) ? it : null;

    public void Clear() => _stack.Clear();

    public void Push(ComponentRuntime runtime)
    {
        if (_stack.TryPeek(out var old))
        {
            if(old == runtime) return;
            old.Leave();
        }
        _stack.Push(runtime);
        runtime.Enter();

        if (_stack.Count > 20) throw new InvalidOperationException("Check stack growth");
    }

    public void Pop()
    {
        if (_stack.Count == 0) return;
        var runtime = _stack.Pop();
        runtime.Leave();
        if (_stack.TryPeek(out var current)) current.Enter();
    }

    public void Replace(ComponentRuntime runtime)
    {
        if (_stack.Count > 0)
        {
            if(_stack.Peek() == runtime) return;

            var oldComponent = _stack.Pop();
            oldComponent.Leave();
        }

        _stack.Push(runtime);
        runtime.Enter();
    }
}

internal sealed class EditorState(ComponentHub componentHub)
{
    private readonly PanelSlot _leftSlot = new();
    private readonly PanelSlot _rightSlot = new();

    public ComponentRuntime? Left => _leftSlot.Current;
    public ComponentRuntime? Right => _rightSlot.Current;

    public void Update()
    {
        if (Left is not null && Left == Right)
        {
            Left.Update();
            return;
        }
        Left?.Update();
        Right?.Update();
    }

    public void UpdateDiagnostic()
    {
        if (Left is not null && Left == Right)
        {
            Left.UpdateDiagnostic();
            return;
        }

        Left?.UpdateDiagnostic();
        Right?.UpdateDiagnostic();
    }

    public void EmitTransition(TransitionMessage msg)
    {
        if (msg.Clear)
        {
            _leftSlot.Clear();
            _rightSlot.Clear();
            return;
        }

        ArgumentNullException.ThrowIfNull(msg.ComponentType);
        switch (msg.Action)
        {
            case TransitionAction.Push: PushPanel(msg.ComponentType, msg.Placement); break;
            case TransitionAction.Pop: PopPanel(msg.Placement); break;
            case TransitionAction.Replace: ReplacePanel(msg.ComponentType, msg.Placement); break;
            default: throw new ArgumentOutOfRangeException(nameof(msg.Action));
        }
    }

    public void PushPanel(Type type, PanelPlacement placement)
    {
        var component = componentHub.Get(type);
        switch (placement)
        {
            case PanelPlacement.Left: _leftSlot.Push(component); break;
            case PanelPlacement.Right: _rightSlot.Push(component); break;
            default: throw new ArgumentOutOfRangeException(nameof(placement));
        }
    }

    public void PopPanel(PanelPlacement placement)
    {
        switch (placement)
        {
            case PanelPlacement.Left: _leftSlot.Pop(); break;
            case PanelPlacement.Right: _rightSlot.Pop(); break;
            default: throw new ArgumentOutOfRangeException(nameof(placement));
        }
    }

    public void ReplacePanel(Type type, PanelPlacement placement)
    {
        var component = componentHub.Get(type);
        switch (placement)
        {
            case PanelPlacement.Left: _leftSlot.Replace(component); break;
            case PanelPlacement.Right: _rightSlot.Replace(component); break;
            default: throw new ArgumentOutOfRangeException(nameof(placement));
        }
    }
}