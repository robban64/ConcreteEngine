using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntity
{
    public DrawEntitySource Source;
    public DrawEntityMeta Meta;
    public EntityId Entity;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntitySource(ModelId model, MaterialTagKey materialKey)
{
    public int InstanceCount;
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialKey;
    public ushort AnimatedSlot;
    public DrawCommandResolver Resolver;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntityMeta(
    DrawCommandId commandId,
    DrawCommandQueue queue,
    PassMask passMask)
{
    public ushort DepthKey;
    public PassMask PassMask = passMask;
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;
}