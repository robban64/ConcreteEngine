using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Rendering.Experimental;





public sealed class DrawCommandSubmitterTree
{
    // 1. RenderTargetId        ex Scene
    // 2. DrawCommandMeta.Id    ex Tilemap
    // 3. DrawCommandMessage 
    /*
    private readonly DrawCommandTargetQueue<DrawCommandMesh> _sceneQueue;
    private readonly DrawCommandTargetQueue<DrawCommandLight> _lightQueue;
    
    internal DrawCommandTargetQueue<DrawCommandMesh> SceneQueue => _sceneQueue;
    internal DrawCommandTargetQueue<DrawCommandLight> LightQueue => _lightQueue;
*/
    
    public readonly TreeSubmitter<DrawCommandMesh> SceneQueue;
    public readonly TreeSubmitter<DrawCommandLight> LightQueue;

    private static readonly RenderTargetId[] RenderTargetIds = Enum.GetValues<RenderTargetId>();
    private static readonly DrawCommandId[] DrawCommandIds = Enum.GetValues<DrawCommandId>();

    public DrawCommandSubmitterTree()
    {
        SceneQueue = new TreeSubmitter<DrawCommandMesh>();
        LightQueue = new TreeSubmitter<DrawCommandLight>();
    }

    public void RegisterCommand(DrawCommandId commandId, RenderTargetId target, int capacity)
    {
        switch (target)
        {
            case RenderTargetId.Scene:
                SceneQueue.Register(target, commandId, capacity);
                break;
            case RenderTargetId.SceneLight:
                LightQueue.Register(target, commandId, capacity);
                break;
            default:
                throw new NotSupportedException(nameof(target));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitMeshDraw(in DrawCommandMesh cmd, in DrawCommandMeta meta) 
        => SceneQueue.SubmitDraw(in cmd, in meta);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitLightDraw(in DrawCommandLight cmd, in DrawCommandMeta meta) 
        => LightQueue.SubmitDraw(in cmd, in meta);


    public void Reset()
    {
        foreach (var target in RenderTargetIds)
        {
            foreach (var cmd in DrawCommandIds)
            {
                SceneQueue.Reset(target, cmd);
                LightQueue.Reset(target, cmd);

            }
        }
    }
}


internal sealed class DrawCommandTargetQueue<T> where T : unmanaged
{
    private readonly DrawCommandMeta[][] _metaQueue;
    private readonly T[][] _dataQueue;
    private readonly int[] _idx;
    
    public int Capacity { get; private set; }
    public RenderTargetId Target { get;  }

    public DrawCommandTargetQueue(RenderTargetId target, int cmdCapacity)
    {
        Capacity = cmdCapacity;
        Target = target;

        _idx = new int[RenderConsts.DrawCommandTypeCount];
        _metaQueue = new DrawCommandMeta[cmdCapacity][];
        _dataQueue = new T[cmdCapacity][];
    }
    
    public void RegisterCommand(DrawCommandId commandId, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfZero(capacity, nameof(capacity));
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4, nameof(capacity));

        if (_metaQueue[(int)commandId] != null)
            throw new InvalidOperationException(
                $"Command {Enum.GetName(commandId)} is already registered at target: {Enum.GetName(Target)}");

        _metaQueue[(int)commandId] = new DrawCommandMeta[capacity];
        _dataQueue[(int)commandId] = new T[capacity];
        _idx[(int)commandId] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw(in T cmd, in DrawCommandMeta meta)
    {
        var index = _idx[(int)meta.Id];
        _dataQueue[(int)meta.Id][index] = cmd;
        _metaQueue[(int)meta.Id][index] = meta;
        _idx[(int)meta.Id]++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<DrawCommandMeta> GetMetaQueue(DrawCommandId commandId) 
        => _metaQueue[(int)commandId].AsSpan(0, _idx[(int)commandId]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetCmdQueue(DrawCommandId commandId) 
        => _dataQueue[(int)commandId].AsSpan(0, _idx[(int)commandId]);


    public void Reset()
    {
        int len = _idx.Length;
        for (int i = 0; i < len; i++)
        {
            _idx[i] = 0;
        }
    }
}