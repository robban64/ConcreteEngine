using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldSky
{
    public ModelId Model { get; private set; }
    public MeshId Mesh { get; private set; }
    public MaterialId Material { get; private set; }

    public bool IsActive => Model > 0 || Material > 0;

    internal WorldSky()
    {
    }

    internal void AttachRenderer(IMeshTable meshTable)
    {
        Mesh = PrimitiveMeshes.SkyboxCube;
        Model = meshTable.CreateSimpleModel(PrimitiveMeshes.SkyboxCube, 0, 0, default);
    }

    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;
}