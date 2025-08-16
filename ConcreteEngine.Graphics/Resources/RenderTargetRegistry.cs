using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics.Resources;

public class RenderTargetRegistry
{
    private ushort _idx = 1;
    private readonly RenderTargetData[] _renderTargets = new RenderTargetData[GraphicsConsts.MaxFboCount];

    public int Count => _idx;

    public RenderTargetKey Add(in RenderTargetData data)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(data.FboId, nameof(data.FboId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(data.Generation, nameof(data.Generation));

        if (_idx >= GraphicsConsts.MaxFboCount - 1)
            GraphicsException.ThrowCapabilityExceeded<GlGraphicsContext>(nameof(_idx),
                _idx, GraphicsConsts.MaxFboCount);

        _renderTargets[_idx - 1] = data;
        return new RenderTargetKey(_idx++);
    }

    public void Get(RenderTargetKey key, out RenderTargetData result)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Key, _idx, nameof(key));

        ref var target = ref _renderTargets[key.Key - 1];

        if (target.FboId == 0 || target.Generation == 0)
            GraphicsException.ThrowResourceNotFound(key.Key);

        result = target;
    }

    public void Replace(RenderTargetKey key, in RenderTargetData newResource, out RenderTargetData oldResource)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Key, _idx, nameof(key));

        Get(key, out oldResource);
        _renderTargets[key.Key - 1] = newResource;
    }

    public void Remove(RenderTargetKey key)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Key, _idx, nameof(key));
        _renderTargets[key.Key - 1] = default;
    }
}