using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.EcsRender;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Graphics.Terrains;

public sealed class Skybox
{
    public static readonly Skybox Current = new();
    public MeshId MeshId { get; } = GfxMeshes.SkyboxCube;
    public Material? Material { get; private set; }

    private RenderEntity _entity;

    private Skybox() { }

    public Id16<Material> MaterialId => Material?.MaterialId ?? Id16<Material>.Empty;

    public void SetMaterial(Material material)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)material.ProfileId, (int)MaterialProfileId.Sky,
            nameof(material));

        Material = material;

        if (!_entity.IsValid)
        {
            _entity = RenderEcs.Core.AddEntity(
                new DrawSource(MeshId, material.MaterialId),
                new DrawPolicy(DrawQueue.Skybox, PassMask.Main));

            RenderEcs.Core.GetWorldBounds(_entity) = default;
        }
        else
        {
            RenderEcs.Core.GetSource(_entity).Material = material.MaterialId;
        }

        RenderEcs.Core.SetStatus(_entity, EntityDrawStatus.AlwaysVisible);
    }
}