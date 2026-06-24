using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class Skybox
{
    public static readonly Skybox Current = new();
    public MeshId MeshId { get; } = GfxMeshes.SkyboxCube;
    public Material? Material { get; private set; }

    private Skybox() { }

    public Id16<MaterialSlot> MaterialId => Material?.MaterialId ?? Id16<MaterialSlot>.Empty;

    public void SetMaterial(Material material)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)material.ProfileId, (int)MaterialProfileId.Sky,
            nameof(material));
        Material = material;
    }
}