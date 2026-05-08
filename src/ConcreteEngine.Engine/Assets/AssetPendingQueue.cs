using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Gateway.Diagnostics;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Engine.Assets;

public sealed record AssetRecreateRequest(AssetId AssetId, AssetKind Kind);

internal sealed class AssetPendingQueue
{
    private readonly Queue<AssetRecreateRequest> _queue = new(8);
    private readonly HashSet<int> _enqueuedIds = new(8);

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

    public void OnFrameStart(long frameId)
    {
        _drainEnabledThisFrame = frameId - _lastDrainFrame >= _intervalFrames;
        if (_drainEnabledThisFrame)
            _lastDrainFrame = frameId;
    }

    public bool Enqueue(AssetRecreateRequest request)
    {
        if (_enqueuedIds.Add(request.AssetId))
        {
            _queue.Enqueue(request);
            return true;
        }

        return false;
    }

    public bool TryDrain(AssetLoader loader, AssetStore store)
    {
        if (_queue.Count == 0) return false;

        if (!_drainEnabledThisFrame)
        {
            _enqueuedIds.Clear();
            return false;
        }

        while (_queue.TryDequeue(out var request))
            OnDrain(loader, store, request);

        return true;
    }

    private bool OnDrain(AssetLoader loader, AssetStore store, AssetRecreateRequest request)
    {
        try
        {
            ProcessRequest(loader, store, request);
            Logger.LogString(LogScope.Engine, $"Recreating: {request}");
            return true;
        }
        catch (Exception ex)
        {
            var msg = $"{ex.GetType().Name}: Error while processing request {request}";
            var level = ErrorUtils.IsUserOrDataError(ex) ? LogLevel.Warn : LogLevel.Critical;
            Logger.LogString(LogScope.Assets, msg, level);
            Logger.LogString(LogScope.Assets, ex.Message, level);

            if (ErrorUtils.IsUserOrDataError(ex) || ex is InvalidOperationException { InnerException: null } ||
                ex is GraphicsException)
            {
                return false;
            }

            throw;
        }
    }

    private void ProcessRequest(AssetLoader loader, AssetStore store, AssetRecreateRequest request)
    {
        if (!loader.IsActive)
            loader.ActivateLazyLoader(request.Kind);

        switch (request.Kind)
        {
            case AssetKind.Shader:
                var shader = store.Get<Shader>(request.AssetId);
                loader.Reload(shader);
                break;
            case AssetKind.Model:
            case AssetKind.Texture:
            case AssetKind.Material:
            case AssetKind.Unknown:
            default:
                throw new ArgumentException($"{request.Kind} is invalid for recreate", nameof(request.Kind));
        }
    }

    public void Clear()
    {
        _queue.Clear();
        _enqueuedIds.Clear();
    }

    public void ForceDrainNow(int frameIndex)
    {
        _drainEnabledThisFrame = true;
        _lastDrainFrame = frameIndex;
    }
}