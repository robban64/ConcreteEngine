namespace ConcreteEngine.Core.Engine.Command;

public interface ICommandSink
{
    void Enqueue(EngineCommandRecord record);
}

