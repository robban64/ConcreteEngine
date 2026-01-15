using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Data;

public delegate void ConsoleCommandDel(ConsoleContext ctx, string action, string? arg1, string? arg2);

public delegate TCommand ConsoleResolveDel<out TCommand>(string action, string? arg1, string? arg2);

public delegate CommandResponse EditorCommandDel<in TCommand>(TCommand cmd, EngineCommandMeta meta)
    where TCommand : EngineCommandRecord;

internal delegate void ComponentActionDel<in TState>(StateContext ctx, ComponentRuntime component, TState state)
    where TState : class;
    
// UI

internal delegate void DrawIterationDel(int i, ref SpanWriter sw);
internal delegate void DrawIterationDel<in T>(int i, T args, ref SpanWriter sw);
 