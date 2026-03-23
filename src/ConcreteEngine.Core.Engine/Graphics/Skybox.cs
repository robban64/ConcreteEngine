using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class Skybox
{
    public static readonly Skybox Instance = new();

    public MeshId MeshId { get; } = GfxMeshes.SkyboxCube;
    public Material? Material { get; private set; }
    
    private Skybox()
    {
    }

    public MaterialId MaterialId => Material?.MaterialId ?? MaterialId.Empty;
    public void SetMaterial(Material material) => Material = material;
}