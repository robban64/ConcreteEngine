using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class Skybox
{
    public MeshId Mesh { get; } = GfxMeshes.SkyboxCube;
    public MaterialId Material { get; private set; }

    public static readonly Skybox Instance = new();

    private Skybox()
    {
    }

    public void SetSkyMaterial(MaterialId materialId) => Material = materialId;
}