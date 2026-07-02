namespace ConcreteEngine.Core.Engine.Command;

public sealed class Commands
{
    public static void Enqueue(EngineCommandRecord record) => _instance._commandSink.Enqueue(record);

    private static Commands _instance = null!;
    internal static void Create(ICommandSink commandBus) => _instance = new Commands(commandBus);
    
    private readonly ICommandSink _commandSink;
    private Commands(ICommandSink commandBus)
    {
        _commandSink = commandBus;
    }
}