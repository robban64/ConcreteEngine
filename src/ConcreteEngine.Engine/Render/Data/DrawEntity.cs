using System.Runtime.InteropServices;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Render.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntity
{
    public DrawEntitySource Source;
    public DrawEntityMeta Meta;
    public RenderEntityId RenderEntity;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntitySource(MeshId mesh, MaterialId material)
{
    public int InstanceCount;
    public MeshId Mesh = mesh;
    public MaterialId Material = material;
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