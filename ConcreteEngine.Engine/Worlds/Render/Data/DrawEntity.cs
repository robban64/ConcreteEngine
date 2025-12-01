#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntityData(in Transform transform, in BoundingBox bounds)
{
    public Transform Transform = transform;
    public BoundingBox Bounds = bounds;
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntity
{
    public EntityId Entity;
    public DrawEntityMeta Meta; 
    public DrawEntitySource Source;
    public bool IsSelected; 

    public DrawEntity(EntityId entity, DrawEntitySource source)
    {
        Entity = entity;
        Source = source;
        Meta = new DrawEntityMeta(DrawCommandId.Model, DrawCommandQueue.Opaque,
            DrawCommandResolver.None, PassMask.Default, 0);
        IsSelected = false;
    }
    
    public readonly DrawEntityMeta FillOut(out ModelId model, out MaterialTagKey materialKey, out ushort animatedSlot)
    {
        model = Source.Model;
        materialKey = Source.MaterialKey;
        animatedSlot = Source.AnimatedSlot;
        return Meta;
    }

    public static DrawEntity Identity => default;

    public void WithDepthKey(ushort depthKey) => Meta.DepthKey = depthKey;

    public void SetAnimationSlot(ushort animationSlot) => Source.AnimatedSlot = animationSlot;
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntitySource(
    ModelId model,
    MaterialTagKey materialKey,
    int drawCount = 0,
    int instanceCount = 0,
    ushort animatedSlot = 0)
{
    public ModelId Model  = model;
    public MaterialTagKey MaterialKey  = materialKey;
    public int DrawCount  = drawCount;
    public int InstanceCount  = instanceCount;
    public ushort AnimatedSlot  = animatedSlot;


}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntityMeta(
    DrawCommandId commandId,
    DrawCommandQueue queue,
    DrawCommandResolver resolver,
    PassMask passMask,
    ushort depthKey)
{
    public DrawCommandId CommandId  = commandId;
    public DrawCommandQueue Queue  = queue;
    public DrawCommandResolver Resolver  = resolver;
    public PassMask PassMask  = passMask;
    public ushort DepthKey  = depthKey;
}