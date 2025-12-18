using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Editor.Definitions;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Render;

namespace ConcreteEngine.Engine.Editor;

internal delegate void EngineQueueCommandDel<in TCommand>(TCommand command)
    where TCommand : class, IEngineCommandRecord;

internal sealed class EditorEngineQueue
{
    private const int QueueLimit = 256;

    private readonly Queue<EngineCommandRecord> _mainCommands = new(8);
    private readonly Queue<EngineCommandRecord> _deferredCommands = new(4);

    private readonly Dictionary<EngineCommandScope, Delegate> _commandHandlers = new(4);

    public int MainCommandCount => _mainCommands.Count;
    public int DeferredCommandCount => _deferredCommands.Count;
    
    public int QueuesCount => _mainCommands.Count + _deferredCommands.Count;

    public EditorEngineQueue(World world, WorldRenderer worldRenderer, AssetSystem assets)
    {
        RegisterHandler<FboCommandRecord>(EngineCommandScope.RenderCommand, worldRenderer.RecreateFrameBuffer);
        RegisterHandler<AssetCommandRecord>(EngineCommandScope.AssetCommand, assets.EnqueueReloadAsset);
        RegisterHandler<IWorldCommandRecord>(EngineCommandScope.WorldCommand, world.ProcessCommand);
    }

    private void RegisterHandler<TCommand>(EngineCommandScope commandScope, EngineQueueCommandDel<TCommand> handler)
        where TCommand : class, IEngineCommandRecord
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
        while (_mainCommands.TryDequeue(out var command))
        {
            switch (command.Scope)
            {
                case EngineCommandScope.WorldCommand:
                    DispatchCommand<IWorldCommandRecord>(command);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void DrainDeferredCommands()
    {
        while (_deferredCommands.TryDequeue(out var command))
        {
            switch (command.Scope)
            {
                case EngineCommandScope.AssetCommand:
                    DispatchCommand<AssetCommandRecord>(command);
                    break;
                case EngineCommandScope.WorldCommand:
                    DispatchCommand<IWorldCommandRecord>(command);
                    break;
                case EngineCommandScope.RenderCommand:
                    DispatchCommand<FboCommandRecord>(command);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void DispatchCommand<TCommand>(EngineCommandRecord command) where TCommand : class, IEngineCommandRecord
    {
        if (command is not TCommand tCommand)
        {
            throw new ArgumentException(
                $"Invalid command type, expected {typeof(TCommand).Name} but got {command.GetType().Name}",
                nameof(command));
        }

        var handler = _commandHandlers[command.Scope];
        ((EngineQueueCommandDel<TCommand>)handler)(tCommand);
    }
}