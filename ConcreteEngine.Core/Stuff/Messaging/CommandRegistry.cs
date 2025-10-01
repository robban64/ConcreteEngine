namespace ConcreteEngine.Core.Stuff.Messaging;

internal delegate void CommandInvoker(IGameCommand command, int tick, EventBus bus);

internal sealed class CommandRegistry
{
    private readonly Dictionary<Type, CommandInvoker> _registry = new();

    public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : IGameCommand
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        if (_registry.ContainsKey(typeof(TCommand)))
            throw new InvalidOperationException($"Command {typeof(TCommand)} already registered");

        _registry[typeof(TCommand)] = (cmd, tick, bus) => handler.Handle((TCommand)cmd, tick, bus);
    }

    public bool TryGetInvoker(Type commandType, out CommandInvoker invoker) =>
        _registry.TryGetValue(commandType, out invoker!);
}