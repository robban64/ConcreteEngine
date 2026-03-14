using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldSky
{
    public MeshId Mesh { get; } = GfxMeshes.SkyboxCube;
    public MaterialId Material { get; private set; }

    internal WorldSky()
    {
    }


    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;
}