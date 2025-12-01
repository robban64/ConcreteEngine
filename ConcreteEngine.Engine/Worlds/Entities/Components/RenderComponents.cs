using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;


public struct RenderSourceComponent(int id, MaterialTagKey materialTagKey, RenderSourceType sourceType)
{
    public int Id = id;
    public MaterialTagKey MaterialKey = materialTagKey;
    public RenderSourceType SourceType = sourceType;
}


public struct ModelComponent(ModelId model, int drawCount) : IRenderSourceComponent
{
    public ModelId Model = model;
    public int DrawCount = drawCount;
    public static RenderSourceType SourceType => RenderSourceType.ModelAsset;
}

public struct ParticleComponent(MeshId mesh, int instanceCount) : IRenderSourceComponent
{
    public MeshId Mesh = mesh;
    public int InstanceCount = instanceCount;
    public static RenderSourceType SourceType => RenderSourceType.DynamicMesh;
}