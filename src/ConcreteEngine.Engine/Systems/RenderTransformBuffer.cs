using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderTransformBuffer : IDisposable
{
    private NativeArray<TransformUniform> _transformBuffer;

    private readonly EntityDataStore _renderData;

    internal RenderTransformBuffer(EntityDataStore renderData)
    {
        ArgumentNullException.ThrowIfNull(renderData);
        _renderData = renderData;
        _transformBuffer = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);
    }

    public NativeView<TransformUniform> Transforms => _transformBuffer.Slice(0, RenderEcs.Core.CullSystem.VisibleCount);

    public void Execute()
    {
        Ensure();
        
        var visibleCount = RenderEcs.Core.CullSystem.VisibleCount;
        if (visibleCount == 0) return;

        var dst = _transformBuffer.Slice(0, visibleCount);

        var src = _renderData.Transforms;
        var sortKeys = _renderData.SortKeys.Slice(0, visibleCount);
        foreach (var it in sortKeys.Zip(dst))
        {
            it.Item2 = src[it.Item1.Entity];
        }
    }
    

    private void Ensure()
    {
        var capacity = RenderEcs.Core.Capacity;
        if (capacity == _transformBuffer.Length) return;

        _transformBuffer.ReAlloc(capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _transformBuffer.Dispose();
    }

}