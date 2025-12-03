#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Data;
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

    public void WithDepthKey(ushort depthKey) => Meta.DepthKey = depthKey;
    public void SetAnimationSlot(int animationSlot) => Source.AnimatedSlot = (ushort)animationSlot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DrawCommandMeta Build(in MeshPart part, bool isTransparent, MaterialId materialId, out DrawCommand cmd)
    {
        cmd = new DrawCommand(part.Mesh, materialId, drawCount: part.DrawCount, animationSlot: Source.AnimatedSlot);
        return isTransparent ? new DrawCommandMeta(Meta.CommandId, DrawCommandQueue.Transparent, Meta.Resolver, Meta.PassMask, (ushort)(ushort.MaxValue - Meta.DepthKey)) : new DrawCommandMeta(Meta.CommandId, Meta.Queue, Meta.Resolver, Meta.PassMask, Meta.DepthKey);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntitySource(
    ModelId model,
    MaterialTagKey materialKey,
    int drawCount = 0,
    int instanceCount = 0,
    ushort animatedSlot = 0)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialKey;
    public int DrawCount = drawCount;
    public int InstanceCount = instanceCount;
    public ushort AnimatedSlot = animatedSlot;
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntityMeta(
    DrawCommandId commandId,
    DrawCommandQueue queue,
    DrawCommandResolver resolver,
    PassMask passMask,
    ushort depthKey)
{
    public int SubmitIndex = -1;
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;
    public DrawCommandResolver Resolver = resolver;
    public PassMask PassMask = passMask;
    public ushort DepthKey = depthKey;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DrawCommandMeta ToCommandMeta() => new (CommandId, Queue, Resolver, PassMask, DepthKey);
}

public struct ExampleStruct
{
    public int X;
    public int Y;
    public int Z;
    public int W = -1;

    public ExampleStruct(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public struct ExampleStructIn(in Vector4 x, in Vector4 y, in Vector4 z)
{
    public Vector4 X  = x;
    public Vector4 Y  = y;
    public Vector4 Z  = z;
    public Vector4 W = new Vector4(-1);
}