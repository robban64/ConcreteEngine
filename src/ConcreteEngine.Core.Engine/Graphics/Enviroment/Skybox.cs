using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Graphics.Enviroment;

public sealed class Skybox
{
    public static readonly Skybox Current = new();
    public MeshId MeshId { get; } = GfxMeshes.SkyboxCube;
    public Material? Material { get; private set; }

    private RenderEntityId _entity;

    private Skybox() { }

    public Id16<Material> MaterialId => Material?.MaterialId ?? Id16<Material>.Empty;

    public void SetMaterial(Material material)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)material.ProfileId, (int)MaterialProfileId.Sky,
            nameof(material));
        
        Material = material;

        if(!_entity.IsValid())
        {
            _entity = RenderEcs.Core.AddEntity(
                new RenderSource(MeshId, material.MaterialId, 0, EntitySourceKind.Model),
                new DrawPolicy(DrawQueue.Skybox, PassMask.Main));
            RenderEcs.Core.GetWorldBounds(_entity) = BoundingBox.Infinite;
        }
        else
        {
            RenderEcs.Core.GetSource(_entity).Material = material.MaterialId;
        }

    }
}