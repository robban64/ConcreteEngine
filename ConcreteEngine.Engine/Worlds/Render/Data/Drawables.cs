#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;
/*
[StructLayout(LayoutKind.Sequential)]
public struct DrawEntity(
    EntityId entity,
    ModelId model,
    MaterialTagKey materialKey,
    in Transform transform,
    DrawCommandMeta meta,
    bool isAnimated)
{
    public Transform Transform = transform;
    public EntityId Entity = entity;
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialKey;
    public DrawCommandMeta Meta = meta;
    public bool IsAnimated = isAnimated;
}
*/

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntityData
{
    public Transform Transform;
    public BoundingBox Bounds;
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntity
{
    public DrawEntityCommandMeta CommandMeta;
    public EntityId Entity;
    public ModelId Model;
    public MaterialTagKey MaterialKey;
    public short AnimatedSlot;
    public byte PartLength;
    public bool IsSelected;
    
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawEntityCommandMeta(
    DrawCommandId commandId,
    DrawCommandQueue queue,
    DrawCommandResolver resolver,
    PassMask passMask,
    ushort depthKey)
{
    public DrawCommandId CommandId { get; init; } = commandId;
    public DrawCommandQueue Queue { get; init;} = queue;
    public DrawCommandResolver Resolver { get; init;} = resolver;
    public PassMask PassMask { get; init;} = passMask;
    public ushort DepthKey { get;init; } = depthKey;

    public void Deconstruct(out DrawCommandId commandId, out DrawCommandQueue queue, out DrawCommandResolver resolver, out PassMask passMask, out ushort depthKey)
    {
        commandId = CommandId;
        queue = Queue;
        resolver = Resolver;
        passMask = PassMask;
        depthKey = DepthKey;
    }
}