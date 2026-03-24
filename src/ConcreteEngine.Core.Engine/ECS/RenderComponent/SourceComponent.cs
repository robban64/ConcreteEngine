using System.Runtime.InteropServices;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    MeshId mesh,
    MaterialId material,
    int meshIndex,
    EntitySourceKind kind,
    DrawCommandQueue queue,
    PassMask mask)
    : IRenderComponent<SourceComponent>
{
    public MeshId Mesh = mesh;
    public MaterialId Material = material;
    public PassMask Mask = mask;
    public byte MeshIndex = (byte)meshIndex;
    public DrawCommandQueue Queue = queue;
    public EntitySourceKind Kind = kind;
}