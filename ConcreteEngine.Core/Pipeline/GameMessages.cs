namespace ConcreteEngine.Core.Pipeline;

public interface IGameMessage
{
    // e.g., "PLACE_BUILDING"
    GameMessageName Name { get; }
    GameMessageMetadata Metadata { get; }
}

public interface IGameCommand : IGameMessage
{
}

public interface IGameEvent : IGameMessage
{
}

internal interface ICommandHandler<in TCommand> where TCommand : IGameCommand
{
    void Handle(TCommand command, int tick, EventBus bus);
}

public readonly struct GameMessageMetadata
{
    public readonly int Id;
    public readonly int CausationId;
    public readonly int CorrelationId;
    public readonly int IntendedTick; // for commands; -1 for events
    public readonly int Source;
    public readonly int Topic;
}