#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntity
{
    public DrawEntitySource Source;
    public DrawEntityMeta Meta;
    public EntityId Entity;

    public void WithDepthKey(ushort depthKey) => Meta.DepthKey = depthKey;

    public void SetAnimationSlot(int animationSlot) => Meta.AnimatedSlot = (ushort)animationSlot;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntitySource(ModelId model, MaterialTagKey materialKey, int drawCount)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialKey;
    public int DrawCount = drawCount;
    public int InstanceCount;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntityMeta(
    DrawCommandId commandId,
    DrawCommandQueue queue,
    PassMask passMask)
{
    public ushort AnimatedSlot;
    public ushort DepthKey;
    public PassMask PassMask = passMask;
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;
    public DrawCommandResolver Resolver;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DrawCommandMeta ToCommandMeta() =>
        new(CommandId, Queue, Resolver, PassMask, DepthKey, AnimatedSlot);
}