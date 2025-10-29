#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

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
    public PassMask PassMask = passMask;
    public int DrawCount = drawCount;
    public ushort DepthKey = depthKey;
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;
}