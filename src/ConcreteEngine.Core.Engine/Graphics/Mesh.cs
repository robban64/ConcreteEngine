using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class Mesh(string name, MeshInfo info)
{
    public readonly string Name = name;
    public readonly MeshInfo Info = info;

    public MeshId MeshId { get; private set; }
    private Matrix4x4 _transform;
    private BoundingBox _bounds;

    public ref readonly Matrix4x4 Transform => ref _transform;
    public ref readonly BoundingBox Bounds => ref _bounds;
    
    internal void SetMeshId(MeshId meshId)
    {
        if (MeshId.IsValid() || !meshId.IsValid()) Throwers.InvalidOperation(nameof(MeshId));
        MeshId = meshId;
    }

    internal void SetTransform(in Matrix4x4 transform) => _transform = transform;
    internal void SetBounds(in BoundingBox bounds) => _bounds = bounds;
}