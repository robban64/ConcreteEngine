using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Gateway.Diagnostics;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics.Error;

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
    private readonly HashSet<int> _ids = new(8);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    
    public bool TryDrain(AssetLoader loader)
    {
        if (_queue.Count == 0) return false;
        
        if (!_drainEnabledThisFrame)
        {
            _ids.Clear();
            return false;
        }

        while (_queue.TryDequeue(out var request))
            OnDrain(loader, in request);

        return true;
    }

    private bool OnDrain(AssetLoader loader, in AssetRecreateRequest rq)
    {
        try
        {
            ProcessRequest(loader, in rq);
            Logger.LogString(LogScope.Engine, $"Recreating: {rq}");
            return true;
        }
        catch (Exception ex)
        {
            var msg = $"{ex.GetType().Name}: Error while processing request {rq.AssetId}";
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

    private void ProcessRequest(AssetLoader loader, in AssetRecreateRequest req)
    {
        if (!loader.IsActive)
            loader.ActivateLazyLoader();

        switch (req.Kind)
        {
            case AssetKind.Shader: loader.ReloadShader(req.AssetId); break;
            case AssetKind.Model:
            case AssetKind.Texture:
            case AssetKind.Material:
            case AssetKind.Unknown:
            default:
                throw new ArgumentException($"{req.Kind} is invalid for recreate", nameof(req.Kind));
        }
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