using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

[StructLayout(LayoutKind.Sequential)]
public struct RenderSourceComponent(ModelId model, int drawCount, MaterialTagKey materialTagKey) 
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public int DrawCount = drawCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(MeshId mesh, int instanceCount, MaterialTagKey materialTagKey) 
{
    public MeshId Mesh  = mesh;
    public MaterialTagKey MaterialTagKey  = materialTagKey;
    public int InstanceCount  = instanceCount;
}