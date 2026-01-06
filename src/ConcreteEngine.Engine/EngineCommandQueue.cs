using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Diagnostics;

namespace ConcreteEngine.Engine;

internal abstract class CommandQueueEntry(CommandScope scope)
{
    public readonly CommandScope Scope = scope;
    public abstract void Dispatch(EngineCommandRecord cmd, EngineCommandContext ctx);
}

internal sealed class CommandQueueEntry<TCommand>(CommandScope scope, Action<TCommand, EngineCommandContext> dispatch)
    : CommandQueueEntry(scope) where TCommand : EngineCommandRecord
{
    public override void Dispatch(EngineCommandRecord cmd, EngineCommandContext ctx)
    {
        if (cmd is not TCommand tCommand)
            throw new InvalidOperationException($"Invalid command type {cmd.Scope}");

        dispatch(tCommand, ctx);
    }
}

internal sealed class EngineCommandQueue
{
    private const int QueueLimit = 16;

    private readonly Queue<EngineCommandPackage> _mainCommands = new(4);
    private readonly Queue<EngineCommandPackage> _deferredCommands = new(4);

    private readonly HashSet<EngineCommandRecord> _commandSet = new(4);

    private readonly Dictionary<CommandScope, CommandQueueEntry> _commandHandlers = new(4);

    public int MainCommandCount => _mainCommands.Count;
    public int DeferredCommandCount => _deferredCommands.Count;
    public int QueuesCount => _mainCommands.Count + _deferredCommands.Count;

    public EngineCommandQueue()
    {
        RegisterHandler<FboCommandRecord>(CommandScope.Render, static (cmd, ctx) => ctx.Renderer.Apply(cmd));
        RegisterHandler<AssetCommandRecord>(CommandScope.Asset, static (cmd, ctx) => ctx.Assets.Apply(cmd));
    }

    private void RegisterHandler<TCommand>(CommandScope commandScope, Action<TCommand, EngineCommandContext> handler)
        where TCommand : EngineCommandRecord
    {
        _commandHandlers.Add(commandScope, new CommandQueueEntry<TCommand>(commandScope, handler));
    }

    public void EnqueueMain(EngineCommandPackage record)
    {
        if (!_commandSet.Add(record.Command))
        {
            Logger.LogString(LogScope.Engine, $"Duplicated command: {record}", LogLevel.Warn);
            return;
        }

        _mainCommands.Enqueue(record);
        if (_mainCommands.Count > QueueLimit)
            throw new InvalidOperationException($"Main commands queue limit exceeded {QueueLimit}");
    }


    public void EnqueueDeferred(EngineCommandPackage record)
    {
        if (!_commandSet.Add(record.Command))
        {
            Logger.LogString(LogScope.Engine, $"Duplicated command: {record}", LogLevel.Warn);
            return;
        }

        _deferredCommands.Enqueue(record);
        if (_deferredCommands.Count > QueueLimit)
            throw new InvalidOperationException($"Deferred commands queue limit exceeded {QueueLimit}");
    }

    public void DrainMainCommands()
    {
    }

    public void DrainDeferredCommands(EngineCommandContext context)
    {
        while (_deferredCommands.TryDequeue(out var package))
        {
            var command = package.Command;
            Logger.LogString(LogScope.Engine, command.ToString(), LogLevel.Info);
            var handler = _commandHandlers[command.Scope];
            _commandSet.Remove(command);
            handler.Dispatch(command, context);
        }
    }
}