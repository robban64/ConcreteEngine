#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Systems;

#endregion

namespace ConcreteEngine.Core.Messaging;

internal sealed class GameMessagePipeline
{
    private readonly EventBus _bus = new();

    //private readonly CommandQueue _queue = new();
    private readonly TypeRegistryCollection<CommandInvoker> _registry = new();

    private readonly List<IGameCommand> _commandQueue = new(128);
    private readonly List<IGameCommand> _commands = new(128);

    public IDisposable Subscribe<TEvent>(Action<IGameEvent> handler) where TEvent : IGameEvent
    {
        return _bus.Subscribe<TEvent>(handler);
    }

    public void Enqueue<TCommand>(TCommand command) where TCommand : IGameCommand
    {
        _commandQueue.Add(command);
    }

    public void RegisterHandler<TCommand>(ICommandHandler<TCommand> handler) where TCommand : IGameCommand
    {
        _registry.Register<TCommand>((cmd, tick, bus) => handler.Handle((TCommand)cmd, tick, bus));
    }

    public void ProcessTick(int tick)
    {
        // apply NextTick subscription changes
        _bus.Prepare();

        // 
        _commands.Clear();
        _commands.AddRange(_commandQueue);
        _commandQueue.Clear();

        foreach (var cmd in _commands)
        {
            var handler = _registry.Get(cmd.GetType());
            handler(cmd, tick, _bus);
        }
    }

    private void Dispatch(object handler, IGameCommand cmd, int tick)
    {
    }

    public void Dispose()
    {
    }

    public void Shutdown()
    {
        
    }
}