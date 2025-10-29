#region

using System.Numerics;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Core.World.Data;
using ConcreteEngine.Core.World.Entities;
using ConcreteEngine.Core.World.Render;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.World;

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

    internal void AttachModelRegistry(IMeshTable meshTable)
    {
        Mesh = PrimitiveMeshes.SkyboxCube;
        Model = meshTable.CreateModel(PrimitiveMeshes.SkyboxCube, 0,0);
    }

    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;

}