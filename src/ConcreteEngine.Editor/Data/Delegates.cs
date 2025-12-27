using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandDel(ConsoleContext ctx, string action, string? arg1, string? arg2);

public delegate TCommand ConsoleResolveDel<out TCommand>(string action, string? arg1, string? arg2);

public delegate CommandResponse EditorCommandDel<in TCommand>(TCommand cmd) where TCommand : EngineCommandRecord;

