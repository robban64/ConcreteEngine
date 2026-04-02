using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Editor.Data;

// Command
public delegate void ConsoleCommandDel(ConsoleContext ctx, string action, string arg1, string arg2);

public delegate TCommand ConsoleResolveDel<out TCommand>(string action, string arg1, string arg2);

public delegate CommandResponse EditorCommandDel<in TCommand>(TCommand cmd, EngineCommandMeta meta)
    where TCommand : EngineCommandRecord;

// UI
internal delegate void ClipDrawDel(int i, FrameContext ctx);

internal delegate void ClipDrawDel<in T>(int i, T args, FrameContext ctx);