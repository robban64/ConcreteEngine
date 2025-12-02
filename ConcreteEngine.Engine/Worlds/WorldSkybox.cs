#region

using System.Numerics;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldSkybox
{
    public ModelId Model { get; private set; }
    public MeshId Mesh { get; private set; }
    public MaterialId Material { get; private set; }
    public readonly Transform Transform = new(Vector3.Zero, Vector3.One, Quaternion.Identity);

    public bool IsActive => Model > 0 || Material > 0;

    internal WorldSkybox()
    {
    }

    internal void AttachRenderer(IMeshTable meshTable)
    {
        Mesh = PrimitiveMeshes.SkyboxCube;
        Model = meshTable.CreateSimpleModel(PrimitiveMeshes.SkyboxCube, 0, 0, default);
    }

    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;
}