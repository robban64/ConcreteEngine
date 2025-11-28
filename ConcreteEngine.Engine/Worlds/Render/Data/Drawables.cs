#region

using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Data;
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
    public EntityId Entity;
    public ModelId Model;
    public MaterialTagKey MaterialKey;
    // process meta (these will be reworked later)
    public byte PartLength;
    public bool IsAnimated;
    public bool IsSelected;
    // renderer meta
    public DrawEntityCommandMeta CommandMeta;
}
// not the actual meta to be uploaded (but contains the base)
// maybe make readonly as it is 2 ushort + 3 byte enums
[StructLayout(LayoutKind.Sequential)]
public struct DrawEntityCommandMeta
{
    public PassMask PassMask;
    public ushort DepthKey;
    public DrawCommandId CommandId;
    public DrawCommandQueue Queue;
    public DrawCommandResolver Resolver;
    /*
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandMeta WithResolvePass(DrawCommandResolver resolver, PassMask passMask) 
        => new (Id, Queue, resolver, passMask, DepthKey);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandMeta WithTransparency(DrawCommandQueue queue, ushort depthKey) 
        => new (Id, queue, Resolver, PassMask, depthKey);
*/
}