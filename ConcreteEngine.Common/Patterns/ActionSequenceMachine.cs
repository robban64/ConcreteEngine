using System.Numerics;

namespace ConcreteEngine.Common.Patterns;

public enum ActionMoveKind
{
    Advance,
    Stay,
    Halt
}

public enum ExecutionStatus
{
    Completed,
    Halted
}

public readonly record struct ExecutionResult(ExecutionStatus Status, int StepsExecuted, int FinalCursor);

public readonly record struct ActionMove(ActionMoveKind Kind, int AdvanceBy = 1)
{
    public static ActionMove Next(int by = 1) => new(ActionMoveKind.Advance, by);
    public static ActionMove Stay() => new(ActionMoveKind.Stay, 0);
    public static ActionMove Halt() => new(ActionMoveKind.Halt, 0);
}

public readonly record struct ActionId(int Value);

public sealed class ActionSequenceMachine
{
    public delegate ActionMove ActionPlain();

    public delegate ActionMove ActionBinding<TCtx>(in TCtx context);

    public delegate bool ConditionBinding<TCtx>(in TCtx context);

    private List<Delegate> _actions = new(4);
    private Dictionary<ConditionId, Delegate> _conditions = new(4);

    public void Run()
    {
        var idx = 0;
    }

    public ActionId RegisterAction(ActionPlain binding)
    {
        ArgumentNullException.ThrowIfNull(binding, nameof(binding));
        var actionId = new ActionId(_actions.Count);
        _actions.Add(binding);
        return actionId;
    }

    public ActionId RegisterAction<TCtx>(ActionBinding<TCtx> binding)
    {
        ArgumentNullException.ThrowIfNull(binding, nameof(binding));
        var actionId = new ActionId(_actions.Count);
        _actions.Add(binding);
        return actionId;
    }

    public void RegisterCondition<TCtx>(int fromId, int toId, ConditionBinding<TCtx> binding)
    {
        ArgumentNullException.ThrowIfNull(binding, nameof(binding));

        if (fromId < 0 || fromId >= _actions.Count)
            throw new ArgumentOutOfRangeException(nameof(fromId));
        if (toId < 0 || toId >= _actions.Count)
            throw new ArgumentOutOfRangeException(nameof(fromId));


        var conditionId = ConditionId.Make(fromId, toId);
        _conditions.Add(conditionId, binding);
    }


    public ExecutionResult Run<TCtx>(in TCtx context)
    {
        int idx = 0, executed = 0;

        while (idx < _actions.Count)
        {
            var action = _actions[idx];

            ActionMove move = default;

            if (action is ActionBinding<TCtx> actionBinding)
                move = actionBinding(in context);
            else if (action is ActionPlain actionPlain)
                move = actionPlain();
            else
                throw new InvalidOperationException($"No binding for ActionId {action} with ctx {typeof(TCtx).Name}");

            executed++;

            if (move.Kind == ActionMoveKind.Stay) continue;
            if (move.Kind == ActionMoveKind.Halt) return new ExecutionResult(ExecutionStatus.Halted, executed, idx);

            if (move.AdvanceBy < 1)
                throw new InvalidOperationException($"Illegal move: AdvanceBy {move.AdvanceBy} at step {idx}.");

            var next = idx + move.AdvanceBy;
            var conditionId = ConditionId.Make(idx, next);

            if (_conditions.TryGetValue(conditionId, out var d) && d is ConditionBinding<TCtx> condition)
            {
                if (!condition(in context)) throw new InvalidOperationException($"Illegal condition {conditionId}.");
            }


            idx = next;
        }

        return new ExecutionResult(ExecutionStatus.Completed, executed, idx);
    }


    internal readonly record struct ConditionId(ActionId ForActionId, ActionId ToActionId)
    {
        public static ConditionId Make(int forActionId, int toActionId) =>
            new(new ActionId(forActionId), new ActionId(toActionId));
    }
}