using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Utility;
using ConcreteEngine.Core.Scene;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderView
{

    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;
    private Matrix4x4 _projectionViewMatrix;

    public ref readonly Matrix4x4 ViewMatrix => ref _viewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref _projectionMatrix;
    public ref readonly Matrix4x4 ProjectionViewMatrix => ref _projectionViewMatrix;

    public Vector3 Position { get; private set; }
    public Vector3 Forward { get; private set; }
    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }
    public ProjectionInfo ProjectionInfo { get; private set; }

    /*
         private RenderViewSnapshot _main;
       private RenderViewSnapshot _override;

         private ref readonly RenderViewSnapshot Active => ref (_useOverride ? ref _override : ref _main);

       public ref readonly Matrix4x4 ViewMatrix => ref Active.ViewMatrix;
       public ref readonly Matrix4x4 ProjectionMatrix => ref Active.ProjectionMatrix;
       public ref readonly Matrix4x4 ProjectionViewMatrix => ref Active.;
       public Vector3 Position => Active.Position;
       public Vector3 Forward => Active.Forward;
       public Vector3 Right => Active.Right;
       public Vector3 Up => Active.Up;
       public ProjectionInfo ProjectionInfo => Active.Projection;
     */
    
    private readonly Snapshot _snapshot = new();

    public RenderView()
    {
    }

    internal void PrepareFrame(in RenderViewSnapshot view)
    {
        _viewMatrix = view.ViewMatrix;
        _projectionMatrix = view.ProjectionMatrix;
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;
        Position = view.Position;
        Up = view.Up;
        Right = view.Right;
        Forward = view.Forward;
        ProjectionInfo = view.ProjectionInfo;
    }

    internal void Restore()
    {
        _viewMatrix = _snapshot.ViewMatrix;
        _projectionMatrix = _snapshot.ProjectionMatrix;
        _projectionViewMatrix = _snapshot.ProjectionViewMatrix;
    }

    internal void ApplyLightView(Vector3 direction)
    {
        _snapshot.Commit(this);
        RenderTransform.CreateDirLightView(direction, this,  out _viewMatrix, out _projectionMatrix, shadowMapSize: 2048);
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;
    }

    private sealed class Snapshot
    {
        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;
        private Matrix4x4 _projectionViewMatrix;

        public ref readonly Matrix4x4 ViewMatrix => ref _viewMatrix;
        public ref readonly Matrix4x4 ProjectionMatrix => ref _projectionMatrix;
        public ref readonly Matrix4x4 ProjectionViewMatrix => ref _projectionViewMatrix;

        public void Commit(RenderView view)
        {
            _viewMatrix = view.ViewMatrix;
            _projectionMatrix = view.ProjectionMatrix;
            _projectionViewMatrix = view.ProjectionViewMatrix;
        }
    }
}