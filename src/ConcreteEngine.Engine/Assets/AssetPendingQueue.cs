using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets;

public readonly struct AssetRecreateRequest(
    int resourceId,
    AssetId assetId,
    AssetKind kind,
    byte priority = 0
)
{
    public readonly int ResourceId = resourceId;
    public readonly AssetId AssetId = assetId;
    public readonly AssetKind Kind = kind;
    public readonly byte Priority = priority;
}

internal sealed class AssetPendingQueue
{
    private readonly Queue<AssetRecreateRequest> _queue = new(8);
    private readonly HashSet<AssetId> _ids = new(8);

    private int _intervalFrames;
    private long _lastDrainFrame;
    private bool _drainEnabledThisFrame;

    public AssetPendingQueue(int intervalFrames = 30)
    {
        _intervalFrames = Math.Max(1, intervalFrames);
        _lastDrainFrame = -_intervalFrames;
    }

    public int Count => _queue.Count;

    public int IntervalFrames
    {
        get => _intervalFrames;
        set => _intervalFrames = Math.Max(1, value);
    }

    public long NextAllowedFrame => _lastDrainFrame + _intervalFrames;

    public void OnFrameStart(long frameId)
    {
        _drainEnabledThisFrame = frameId - _lastDrainFrame >= _intervalFrames;
        if (_drainEnabledThisFrame)
            _lastDrainFrame = frameId;
    }

    public bool Enqueue(in AssetRecreateRequest request)
    {
        if (_ids.Add(request.AssetId))
        {
            _queue.Enqueue(request);
            return true;
        }

        return false;
    }

    public bool TryDrain(out AssetRecreateRequest request)
    {
        if (!_drainEnabledThisFrame || _queue.Count == 0)
        {
            _ids.Clear();
            request = default;
            return false;
        }

        request = _queue.Dequeue();
        return true;
    }

    public void Clear()
    {
        _queue.Clear();
        _ids.Clear();
    }

    public void ForceDrainNow(int frameIndex)
    {
        _drainEnabledThisFrame = true;
        _lastDrainFrame = frameIndex;
    }
}