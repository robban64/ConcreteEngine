namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandReqDel(DebugConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandReqDel<TPayload>(in TPayload payload);

// editor view delegates
public delegate bool FetchCameraDataRequest(long generation,out CameraEditorPayload response);

