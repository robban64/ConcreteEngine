#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Graphics.Gfx.Resources;
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
public struct DrawSpecialEntity
{
    public EntityId Entity;
    public DrawEntityMeta Meta;
    public MeshId Mesh;
    public MaterialId Material;
    public int DrawCount;
    public int InstanceCount;
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
    public readonly DrawCommandMeta Build(in MeshPart part, bool isTransparent, MaterialId materialId,
        out DrawCommand cmd)
    {
        cmd = new DrawCommand(part.Mesh, materialId, drawCount: part.DrawCount, animationSlot: Source.AnimatedSlot);
        return isTransparent
            ? new DrawCommandMeta(Meta.CommandId, DrawCommandQueue.Transparent, Meta.Resolver, Meta.PassMask,
                (ushort)(ushort.MaxValue - Meta.DepthKey))
            : new DrawCommandMeta(Meta.CommandId, Meta.Queue, Meta.Resolver, Meta.PassMask, Meta.DepthKey);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntitySource(ModelId model, MaterialTagKey materialKey, RenderSourceKind kind, int drawCount)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialKey;
    public int DrawCount = drawCount;
    public int InstanceCount;
    public ushort AnimatedSlot;
    public RenderSourceKind Kind = kind;
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntityMeta(
    DrawCommandId commandId,
    DrawCommandQueue queue,
    DrawCommandResolver resolver,
    PassMask passMask,
    ushort depthKey)
{
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;
    public DrawCommandResolver Resolver = resolver;
    public PassMask PassMask = passMask;
    public ushort DepthKey = depthKey;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DrawCommandMeta ToCommandMeta() => new(CommandId, Queue, Resolver, PassMask, DepthKey);
}