#region

using System.Numerics;
using ConcreteEngine.Renderer.Utility;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Renderer.State;

public sealed class RenderView
{
    private RenderViewSnapshot _snapshot;
    private TransformMatrixData _override;

    private bool _useOverride = false;

    public ref readonly Matrix4x4 ViewMatrix => ref _useOverride ? ref _override.ModelMatrix : ref _snapshot.ViewMatrix;

    public ref readonly Matrix4x4 ProjectionMatrix =>
        ref _useOverride ? ref _override.ProjectionMatrix : ref _snapshot.ProjectionMatrix;

    public ref readonly Matrix4x4 ProjectionViewMatrix =>
        ref _useOverride ? ref _override.ProjectionViewMatrix : ref _snapshot.ProjectionViewMatrix;

    public ProjectionInfoData ProjectionInfo => _snapshot.ProjectionInfo;
    public Vector3 Position => _snapshot.Position;
    public Vector3 Forward => _snapshot.Forward;
    public Vector3 Right => _snapshot.Right;
    public Vector3 Up => _snapshot.Up;


    public void GetCurrentData(out Matrix4x4 view, out Matrix4x4 projection, out Matrix4x4 projectionView)
    {
        if (_useOverride)
        {
            view = _override.ModelMatrix;
            projection = _override.ProjectionMatrix;
            projectionView = _override.ProjectionViewMatrix;
            return;
        }

        view = _snapshot.ViewMatrix;
        projection = _snapshot.ProjectionMatrix;
        projectionView = _snapshot.ProjectionViewMatrix;
    }

    public void SetViewData(in RenderViewSnapshot view)
    {
        _useOverride = false;
        _snapshot = view;
    }

    internal void ClearOverride() => _useOverride = false;

    internal void ApplyLightViewOverride(Vector3 direction, RenderParamsSnapshot paramsSnapshot)
    {
        ref readonly var shadow = ref paramsSnapshot.Shadows;

        RenderTransform.CreateDirLightView(direction, in _snapshot, out var viewMat, out var projMat,
            shadowMapSize: shadow.ShadowMapSize, shadowDistance: shadow.Distance);

        var projViewMat = viewMat * projMat;
        
        _override = new TransformMatrixData(in viewMat, in projMat, in projViewMat);
        _useOverride = true;
    }
}