using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Common.Patterns;

public enum ActionMoveKind
{
    Advance = 0,
    Stay = 1,
    Halt = 2
}

public enum ExecutionStatus
{
    Completed = 0,
    Halted = 1
}

public readonly record struct ExecutionResult(ExecutionStatus Status, int StepsExecuted, int FinalCursor);

public readonly record struct ActionMove(ActionMoveKind Kind, int AdvanceBy = 1)
{
    public static ActionMove Next(int by = 1) => new(ActionMoveKind.Advance, by);
    public static ActionMove Stay() => new(ActionMoveKind.Stay, 0);
    public static ActionMove Halt() => new(ActionMoveKind.Halt, 0);
}

public readonly record struct ActionId(int Value);

public sealed class ActionSequenceMachine<TCtx>
{
    public delegate ActionMove ActionBinding(TCtx context);
    public delegate bool ConditionBinding(TCtx context);

    private readonly Dictionary<ConditionId, ConditionBinding> _conditions = new(4);

    private readonly List<ActionBinding> _registerActions = new(4);

    private bool _froozen = false;
    
    public ActionId RegisterAction(ActionBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding, nameof(binding));
        if (_froozen) throw new InvalidOperationException(nameof(_froozen));

        var actionId = new ActionId(_registerActions.Count);
        _registerActions.Add(binding);
        return actionId;
    }

    public void RegisterCondition(int fromId, int toId, ConditionBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding, nameof(binding));
        if (_froozen) throw new InvalidOperationException(nameof(_froozen));

        if (fromId < 0 || fromId >= _registerActions.Count)
            throw new ArgumentOutOfRangeException(nameof(fromId));
        if (toId < 0 || toId >= _registerActions.Count)
            throw new ArgumentOutOfRangeException(nameof(fromId));


        var conditionId = ConditionId.Make(fromId, toId);
        _conditions.Add(conditionId, binding);
    }


    public ExecutionResult Run(TCtx context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentOutOfRangeException.ThrowIfLessThan(_registerActions.Count, 2);
        
        _froozen = true;
        int idx = 0, executed = 0, actionExecuted = 0;
        var span = CollectionsMarshal.AsSpan(_registerActions);
        int length = span.Length;

        while (idx < length)
        {
            var move = span[idx](context);
            executed++;
            actionExecuted++;

            switch (move.Kind)
            {
                case ActionMoveKind.Advance:
                {
                    if (move.AdvanceBy < 1)
                        ThrowIllegalAdvance(move.AdvanceBy, idx);
                    var next = idx + move.AdvanceBy;
                    var conditionId = ConditionId.Make(idx, next);


                    if (_conditions.TryGetValue(conditionId, out var cond))
                    {
                        if (!cond(context)) ThrowIllegalConditionPacked(idx, next);
                    }

                    idx = next;
                    break;
                }
                case ActionMoveKind.Stay:
                    if (actionExecuted > 1000) ThrowIllegalStayAmount(actionExecuted);
                    continue;
                case ActionMoveKind.Halt: return new ExecutionResult(ExecutionStatus.Halted, executed, idx);
                default: ThrowUnknownMove(move.Kind); break;
            }

            actionExecuted = 0;
        }

        return new ExecutionResult(ExecutionStatus.Completed, executed, idx);
    }


    internal readonly record struct ConditionId(ActionId ForActionId, ActionId ToActionId)
    {
        public static ConditionId Make(int forActionId, int toActionId) =>
            new(new ActionId(forActionId), new ActionId(toActionId));
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn]
    static void ThrowIllegalStayAmount(int stayAmount) =>
        throw new InvalidOperationException($"Illegal stay: Stayed for max {stayAmount}.");

    
    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn]
    static void ThrowIllegalAdvance(int advanceBy, int idx) =>
        throw new InvalidOperationException($"Illegal move: AdvanceBy {advanceBy} at step {idx}.");

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn]
    static void ThrowIllegalConditionPacked(int from, int to) =>
        throw new InvalidOperationException($"Illegal condition ({from}->{to}).");

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn]
    static void ThrowUnknownMove(ActionMoveKind kind) =>
        throw new InvalidOperationException($"Unknown move kind: {kind}.");

}