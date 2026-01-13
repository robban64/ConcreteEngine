using System.Runtime.InteropServices;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    MeshId mesh,
    MaterialId material,
    int modelMeshIndex,
    EntitySourceKind kind,
    DrawCommandQueue queue,
    PassMask mask)
    : IRenderComponent<SourceComponent>
{
    public MeshId Mesh = mesh;
    public MaterialId Material = material;
    public PassMask Mask = mask;
    public byte ModelMeshIndex = (byte)modelMeshIndex;
    public DrawCommandQueue Queue = queue;
    public EntitySourceKind Kind = kind;
}