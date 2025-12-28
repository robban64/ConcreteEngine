using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Metadata.Command;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EditorEngineQueue
{
    private const int QueueLimit = 256;

    private readonly Queue<EngineCommandRecord> _mainCommands = new(8);
    private readonly Queue<EngineCommandRecord> _deferredCommands = new(4);

    private readonly Dictionary<CommandScope, Delegate> _commandHandlers = new(4);

    public int MainCommandCount => _mainCommands.Count;
    public int DeferredCommandCount => _deferredCommands.Count;

    public int QueuesCount => _mainCommands.Count + _deferredCommands.Count;

    public EditorEngineQueue(World world, AssetSystem assets)
    {
        RegisterHandler<RenderCommandRecord>(CommandScope.Render, world.RecreateFrameBuffer);
        RegisterHandler<AssetCommandRecord>(CommandScope.Asset, assets.EnqueueReloadAsset);
    }

    private void RegisterHandler<TCommand>(CommandScope commandScope, Action<TCommand> handler)
        where TCommand : EngineCommandRecord
    {
        _commandHandlers.Add(commandScope, handler);
    }

    public void EnqueueMain(EngineCommandRecord record)
    {
        _mainCommands.Enqueue(record);
        if (_mainCommands.Count > QueueLimit)
            throw new InvalidOperationException($"Main commands queue limit exceeded {QueueLimit}");
    }

    public void EnqueueDeferred(EngineCommandRecord record)
    {
        _deferredCommands.Enqueue(record);
        if (_deferredCommands.Count > QueueLimit)
            throw new InvalidOperationException($"Deferred commands queue limit exceeded {QueueLimit}");
    }

    public void DrainMainCommands()
    {
        /*
        while (_mainCommands.TryDequeue(out var command))
        {
            switch (command.Scope)
            {
                case EngineCommandScope.WorldCommand:
                    DispatchCommand<IWorldCommandRecord>(command);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }*/
    }

    public void DrainDeferredCommands()
    {
        while (_deferredCommands.TryDequeue(out var command))
        {
            switch (command.Scope)
            {
                case CommandScope.Asset:
                    DispatchCommand((AssetCommandRecord)command);
                    break;
                case CommandScope.Render:
                    DispatchCommand((RenderCommandRecord)command);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void DispatchCommand<TCommand>(TCommand command) where TCommand : EngineCommandRecord
    {
        var handler = _commandHandlers[command.Scope];
        ((Action<TCommand>)handler)(command);
    }
}