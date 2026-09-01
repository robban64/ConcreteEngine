using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS.Render;
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
        bool isValid = _entity.IsValid;
        if (!isValid)
        {
            _entity = RenderEcs.Core.AddEntity(
                new DrawSource(MeshId, material.MaterialId),
                new DrawPolicy(DrawQueue.Skybox, PassMask.Main));
            
        }
        
        var context = RenderEcs.Core.GetEntityContext(_entity);
        if(isValid)
            context.Source.Material = material.MaterialId;

        context.SetStatus(EntityDrawStatus.AlwaysVisible);
    }
}