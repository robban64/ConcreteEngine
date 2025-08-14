using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;


public enum DrawCommandId : short
{
    Tilemap,
    Sprite
}

public readonly struct DrawCommandMessage(in DrawCommandData cmd, in DrawCommandMeta info)
{
    public readonly DrawCommandData Cmd = cmd;
    public readonly DrawCommandMeta Info = info;
}

public readonly struct DrawCommandData(
    ushort meshId,
    MaterialId materialId,
    uint drawCount,
    in Matrix4X4<float> transform)
{
    public readonly ushort MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly uint DrawCount = drawCount;

    public readonly Matrix4X4<float> Transform = transform;
}

public readonly struct DrawCommandMeta(DrawCommandId id, RenderTargetId target, short layer)
{
    public readonly DrawCommandId Id = id;
    public readonly RenderTargetId Target = target;
    public readonly short Layer = layer;
}