using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldSky
{
    public MeshId Mesh { get; }
    public MaterialId Material { get; private set; }

    internal WorldSky()
    {
        Mesh = PrimitiveMeshes.SkyboxCube;
    }


    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;
}