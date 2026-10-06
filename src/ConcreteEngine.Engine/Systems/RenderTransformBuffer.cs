using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Render;
using ConcreteEngine.Core.Engine.Render.Systems;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderTransformBuffer : IDisposable
{
    private NativeArray<TransformUniform> _transformBuffer;

    internal RenderTransformBuffer()
    {
        var capacity = RenderWorld.Instance.Capacity;
        if(capacity == 0) Throwers.InvalidOperation(nameof(capacity));
        _transformBuffer = NativeArray.AlignedAllocate<TransformUniform>(RenderWorld.Instance.Capacity, 64, false);
    }

    public NativeView<TransformUniform> Transforms =>
        _transformBuffer.Slice(0, RenderWorld.System<RenderCullSystem>().VisibleCount);

    public void Execute()
    {
        Ensure();

        var visibleCount = RenderWorld.System<RenderCullSystem>().VisibleCount;
        if (visibleCount == 0) return;

        var dst = _transformBuffer.AsSpan(0, visibleCount);
        var sortKeys = RenderWorld.Dense<DrawEntityKey>().AsReadOnlySpan().Slice(0, visibleCount);

        var srcTransforms = MemoryMarshal.Cast<WorldTransform, Matrix4x4>(RenderWorld.Dense<WorldTransform>().AsReadOnlySpan());
        var srcNormals = MemoryMarshal.Cast<NormalMatrix, Matrix3X4>(RenderWorld.Dense<NormalMatrix>().AsReadOnlySpan());
        
        for (int i = 0; i < visibleCount; i++)
        {
            var entityKey = sortKeys[i];
            dst[i].Model = srcTransforms[entityKey.Entity];
            dst[i].Normal = srcNormals[entityKey.Entity];
        }

    }

    private void Ensure()
    {
        var capacity = RenderWorld.Instance.Capacity;
        if (capacity == _transformBuffer.Length) return;

        _transformBuffer.ReAlloc(capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _transformBuffer.Dispose();
    }
}