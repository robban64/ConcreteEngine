using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Engine.Assets;

public sealed record AssetRecreateRequest(AssetId AssetId, AssetKind Kind);

internal sealed class AssetPendingQueue
{
    private readonly Queue<AssetRecreateRequest> _queue = new(8);
    private readonly HashSet<int> _enqueuedIds = new(8);

    public int Count => _queue.Count;


    public bool Enqueue(AssetRecreateRequest request)
    {
        if (!_enqueuedIds.Add(request.AssetId.Value))
        {
            Logger.LogString(LogScope.Assets, $"Asset already in pending queue: {request.AssetId.Value}");
            return false;
        }

        _queue.Enqueue(request);
        return true;
    }

    public bool TryDrain(AssetLoader loader, AssetStore store)
    {
        if (_queue.Count == 0) return false;

        while (_queue.TryDequeue(out var request))
        {
            _enqueuedIds.Remove(request.AssetId.Value);
            OnDrain(loader, store, request);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool OnDrain(AssetLoader loader, AssetStore store, AssetRecreateRequest request)
    {
        Logger.LogString(LogScope.Assets, $"Recreating: {request}");

        try
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
}