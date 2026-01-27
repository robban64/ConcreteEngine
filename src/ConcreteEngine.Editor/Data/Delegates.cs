using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;

namespace ConcreteEngine.Editor.Data;

// Command
public delegate void ConsoleCommandDel(ConsoleContext ctx, string action, string arg1, string arg2);

public delegate TCommand ConsoleResolveDel<out TCommand>(string action, string arg1, string arg2);

public delegate CommandResponse EditorCommandDel<in TCommand>(TCommand cmd, EngineCommandMeta meta)
    where TCommand : EngineCommandRecord;

// Search
public delegate bool SearchSceneObjectDel(in SearchStringPacked search, SceneObjectFilter filter, in SceneObjectItem item);
public delegate bool SearchAssetDel(in SearchStringPacked search, SearchAssetFilter filter, IAsset asset);


// UI
internal delegate void ClipDrawDel(int i, in FrameContext ctx);

internal delegate void ClipDrawDel<in T>(int i, T args, in FrameContext ctx);