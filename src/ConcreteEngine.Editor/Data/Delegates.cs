using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ZaString.Core;

namespace ConcreteEngine.Editor.Data;

public delegate void ConsoleCommandDel(ConsoleContext ctx, string action, string? arg1, string? arg2);

public delegate TCommand ConsoleResolveDel<out TCommand>(string action, string? arg1, string? arg2);

public delegate CommandResponse EditorCommandDel<in TCommand>(TCommand cmd, EngineCommandMeta meta)
    where TCommand : EngineCommandRecord;

internal delegate void ComponentActionDel<in TState>(GlobalContext ctx, ComponentRuntime component, TState state)
    where TState : class;
    
// Ui    
internal delegate void DrawRowDel<in T>(int i, T body, ref Span<byte> buffer);
