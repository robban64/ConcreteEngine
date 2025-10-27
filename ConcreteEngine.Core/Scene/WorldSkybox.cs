#region

using System.Numerics;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Core.RenderingSystem.Primitives;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class WorldSkybox
{
    // private MeshId _meshId;
    private ModelId _modelId;
    private MaterialId _materialId;

    private Transform _transform = new(Vector3.Zero, Vector3.One, Quaternion.Identity);

    public bool IsActive => _modelId > 0 || _materialId > 0;

    internal WorldSkybox()
    {
        //_meshId = PrimitiveMeshes.SkyboxCube;
    }

    internal void AttachModelRegistry(IModelRegistry modelRegistry)
    {
        _modelId = modelRegistry.CreateModel(PrimitiveMeshes.SkyboxCube, 0);
    }

    public void SetSkyMaterial(MaterialId materialId) => _materialId = materialId;

    internal void GetDrawEntity(out DrawEntity drawEntity)
    {
        var model = new ModelComponent(_modelId, _materialId, 0);
        EntityUtility.MakeSkybox(model, in _transform, out drawEntity);
    }
}