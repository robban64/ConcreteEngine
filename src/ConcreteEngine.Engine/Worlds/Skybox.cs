using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Worlds;

public sealed class Skybox
{
    public MeshId Mesh { get; } = GfxMeshes.SkyboxCube;
    public MaterialId Material { get; private set; }

    internal Skybox()
    {
    }


    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;
}