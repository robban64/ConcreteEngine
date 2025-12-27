using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Patterns;

public static class ActionSequenceMachine
{
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

    public readonly struct ExecutionResult(ExecutionStatus status, int stepsExecuted, int finalCursor)
    {
        public readonly int StepsExecuted = stepsExecuted;
        public readonly int FinalCursor = finalCursor;
        public readonly ExecutionStatus Status = status;
    }

    public readonly struct ActionMove(ActionMoveKind kind, int advanceBy = 1)
    {
        public readonly int AdvanceBy = advanceBy;
        public readonly ActionMoveKind Kind = kind;

        public static ActionMove Next(int by = 1) => new(ActionMoveKind.Advance, by);
        public static ActionMove Stay() => new(ActionMoveKind.Stay, 0);
        public static ActionMove Halt() => new(ActionMoveKind.Halt, 0);
    }

    public readonly record struct ActionId(int Value);
}

public sealed class ActionSequenceMachine<TCtx>
{
    public delegate ActionSequenceMachine.ActionMove ActionBinding(TCtx context);

    public delegate bool ConditionBinding(TCtx context);

    private readonly Dictionary<ConditionId, ConditionBinding> _conditions = new(4);

    private readonly List<ActionBinding> _registerActions = new(4);

    private bool _frozen;

    public ActionSequenceMachine.ActionId RegisterAction(ActionBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (_frozen) throw new InvalidOperationException(nameof(_frozen));

        var actionId = new ActionSequenceMachine.ActionId(_registerActions.Count);
        _registerActions.Add(binding);
        return actionId;
    }

    public void RegisterCondition(int fromId, int toId, ConditionBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (_frozen) throw new InvalidOperationException(nameof(_frozen));

        if (fromId < 0 || fromId >= _registerActions.Count)
            throw new ArgumentOutOfRangeException(nameof(fromId));
        if (toId < 0 || toId >= _registerActions.Count)
            throw new ArgumentOutOfRangeException(nameof(fromId));


        var conditionId = ConditionId.Make(fromId, toId);
        _conditions.Add(conditionId, binding);
    }


    public ActionSequenceMachine.ExecutionResult Run(TCtx context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        ArgumentOutOfRangeException.ThrowIfLessThan(_registerActions.Count, 2);

        _frozen = true;
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
                case ActionSequenceMachine.ActionMoveKind.Advance:
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
                case ActionSequenceMachine.ActionMoveKind.Stay:
                    if (actionExecuted > 1000) ThrowIllegalStayAmount(actionExecuted);
                    continue;
                case ActionSequenceMachine.ActionMoveKind.Halt:
                    return new ActionSequenceMachine.ExecutionResult(ActionSequenceMachine.ExecutionStatus.Halted,
                        executed, idx);
                default: ThrowUnknownMove(move.Kind); break;
            }

            actionExecuted = 0;
        }

        return new ActionSequenceMachine.ExecutionResult(ActionSequenceMachine.ExecutionStatus.Completed, executed,
            idx);
    }


    internal readonly record struct ConditionId(
        ActionSequenceMachine.ActionId ForActionId,
        ActionSequenceMachine.ActionId ToActionId)
    {
        public static ConditionId Make(int forActionId, int toActionId) =>
            new(new ActionSequenceMachine.ActionId(forActionId), new ActionSequenceMachine.ActionId(toActionId));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowIllegalStayAmount(int stayAmount) =>
        throw new InvalidOperationException($"Illegal stay: Stayed for max {stayAmount}.");


    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowIllegalAdvance(int advanceBy, int idx) =>
        throw new InvalidOperationException($"Illegal move: AdvanceBy {advanceBy} at step {idx}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowIllegalConditionPacked(int from, int to) =>
        throw new InvalidOperationException($"Illegal condition ({from}->{to}).");

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowUnknownMove(ActionSequenceMachine.ActionMoveKind kind) =>
        throw new InvalidOperationException($"Unknown move kind: {kind}.");
}