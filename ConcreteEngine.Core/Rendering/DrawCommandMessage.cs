#region

using System.Numerics;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum DrawCommandId : byte
{
    Tilemap,
    Sprite,
    Effect
}

public enum DrawCommandKind : byte
{
    Mesh,
    Light
}


public readonly struct DrawCommandMesh(
    MeshId meshId,
    MaterialId materialId,
    uint drawCount,
    in Matrix4x4 transform)
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly uint DrawCount = drawCount;
    public readonly Matrix4x4 Transform = transform;
}

public struct DrawCommandLight(Vector2 position, float radius, Vector3 color, float intensity)
{
    public Vector2 Position = position; 
    public float   Radius = radius;   
    public Vector3 Color = color;    
    public float   Intensity = intensity;
}

public readonly struct DrawCommandMeta(DrawCommandId id, RenderTargetId target,DrawCommandKind kind,  byte layer)
{
    public readonly DrawCommandId Id = id;
    public readonly RenderTargetId Target = target;
    //public readonly RenderPassOp Pass = pass;
    public readonly DrawCommandKind Kind = kind;
    public readonly byte Layer = layer;
}