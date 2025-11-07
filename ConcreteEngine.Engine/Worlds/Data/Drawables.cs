#region

using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntity(
    EntityId entity,
    ModelId model,
    MaterialTagKey materialKey,
    int drawCount,
    in Transform transform,
    DrawCommandId commandId,
    DrawCommandQueue queue,
    PassMask passMask,
    ushort depthKey = 0)
{
    public Transform Transform = transform;
    public EntityId Entity = entity;
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialKey;
    public int DrawCount = drawCount;
    public PassMask PassMask = passMask;
    public ushort DepthKey = depthKey;
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;
}