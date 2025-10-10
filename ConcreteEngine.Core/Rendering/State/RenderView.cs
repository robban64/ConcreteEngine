using System.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Utility;

namespace ConcreteEngine.Core.Rendering.State;

public sealed class RenderView
{
    private RenderViewSnapshot _snapshot;
    private ViewProjectionData _override;

    private bool _useOverride = false;

    public ref readonly Matrix4x4 ViewMatrix
        => ref (_useOverride ? ref _override.ViewMatrix : ref _snapshot.ViewMatrix);

    public ref readonly Matrix4x4 ProjectionMatrix
        => ref (_useOverride ? ref _override.ProjectionMatrix : ref _snapshot.ProjectionMatrix);

    public ref readonly Matrix4x4 ProjectionViewMatrix
        => ref (_useOverride ? ref _override.ProjectionViewMatrix : ref _snapshot.ProjectionViewMatrix);

    public ProjectionInfo ProjectionInfo => _snapshot.ProjectionInfo;
    public Vector3 Position => _snapshot.Position;
    public Vector3 Forward => _snapshot.Forward;
    public Vector3 Right => _snapshot.Right;
    public Vector3 Up => _snapshot.Up;


    public RenderView()
    {
    }

    internal void GetCurrentData(out Matrix4x4 view, out Matrix4x4 projection, out Matrix4x4 projectionView)
    {
        if (_useOverride)
        {
            view = _override.ViewMatrix;
            projection = _override.ProjectionMatrix;
            projectionView = _override.ProjectionViewMatrix;
            return;
        }

        view = _snapshot.ViewMatrix;
        projection = _snapshot.ProjectionMatrix;
        projectionView = _snapshot.ProjectionViewMatrix;
    }

    internal void PrepareFrame(in RenderViewSnapshot view)
    {
        _useOverride = false;
        _snapshot = view;
    }

    internal void ClearOverride() => _useOverride = false;

    internal void ApplyLightViewOverride(Vector3 direction)
    {
        RenderTransform.CreateDirLightView(direction, in _snapshot, out var viewMat, out var projMat, shadowMapSize: 2048);
        var projViewMat = viewMat * projMat;
        _override = new ViewProjectionData(in viewMat, in projMat, in projViewMat);
        _useOverride = true;
    }
}