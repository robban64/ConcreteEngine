#region

using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor.Data;

// State Delegates
internal delegate void StateTransitionDel<TModel>(ModelState<TModel> ctx, TModel state) where TModel : class;

internal delegate void StateEmptyEventDel<TModel>(ModelState<TModel> ctx) where TModel : class;

internal delegate void StateEventDel<TModel, in TEvent>(ModelState<TModel> ctx, TEvent ev) where TModel : class;

// command delegates
public delegate void ConsoleCommandReqDel(ConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandDel<TPayload>(in TPayload payload);

public delegate CommandResponse EditorDataCommandDel<TRequest>(in TRequest request, out TRequest response)
    where TRequest : unmanaged;

// Request delegates
public delegate TResponse? ApiModelRequestDel<in TRequest, out TResponse>(TRequest request)
    where TRequest : class where TResponse : class;

public delegate void ApiDataRequest<TRequest>(in TRequest request, out TRequest response)
    where TRequest : unmanaged;

public delegate void ApiRefRequestDel<TRequest>(ref TRequest request) where TRequest : unmanaged;

public readonly unsafe struct ApiDataRefRequest<T>(
    delegate*<ApiWriteRequestBody<T>, long> fillData,
    delegate*<ApiWriteRequestBody<T>, long> writeData)
    where T : unmanaged
{
    public long FillData(long version, ref T data) => fillData(new ApiWriteRequestBody<T>(version, ref data));
    public long WriteData(long version, ref T data) => writeData(new ApiWriteRequestBody<T>(version, ref data));
}

public ref struct ApiWriteRequestBody<TReq>(long version, ref TReq data) where TReq : unmanaged
{
    public readonly long Version = version;
    public ref TReq Data = ref data;
}