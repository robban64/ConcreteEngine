#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Core.Assets;


public readonly record struct RecreateRequest(
    int ResourceId,
    AssetId AssetId,
    AssetKind Kind,
    byte Priority = 0
);

internal sealed class ResourcePendingQueue
{
    private readonly Queue<RecreateRequest> _queue = new(8);
    private readonly HashSet<AssetId> _ids = new(8);

    private int _intervalFrames;
    private long _lastDrainFrame;
    private bool _drainEnabledThisFrame;

    public ResourcePendingQueue(int intervalFrames = 30)
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

    public void OnFrameStart(long frameIndex)
    {
        _drainEnabledThisFrame = (frameIndex - _lastDrainFrame) >= _intervalFrames;
        if (_drainEnabledThisFrame)
            _lastDrainFrame = frameIndex;
    }

    public bool Enqueue(in RecreateRequest request)
    {
        if (_ids.Add(request.AssetId))
        {
            _queue.Enqueue(request);
            return true;
        }

        return false;
    }

    public bool TryDrain(out RecreateRequest request)
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