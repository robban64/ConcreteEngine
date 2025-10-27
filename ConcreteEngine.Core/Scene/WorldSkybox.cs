#region

using System.Numerics;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Core.RenderingSystem.Primitives;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class WorldSkybox
{
    public ModelId Model { get; private set; }
    public MeshId Mesh { get; private set; }
    public MaterialId Material { get; private set; }
    public Transform Transform  { get; private set; } = new(Vector3.Zero, Vector3.One, Quaternion.Identity);
    
    public bool IsActive => Model > 0 || Material > 0;

    internal WorldSkybox()
    {
    }

    internal void AttachModelRegistry(IModelRegistry modelRegistry)
    {
        Mesh = PrimitiveMeshes.SkyboxCube;
        Model = modelRegistry.CreateModel(PrimitiveMeshes.SkyboxCube, 0,0);
    }

    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;

}