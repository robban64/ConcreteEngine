#region

using System.Numerics;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Utils;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class WorldSkybox
{
    private MeshId _meshId;
    private MaterialId _materialId;

    private Transform _transform = new(Vector3.Zero, Vector3.One, Quaternion.Identity);

    public bool IsActive => _meshId > 0 || _materialId > 0;

    internal WorldSkybox()
    {
        _meshId = PrimitiveMeshes.SkyboxCubeMesh;
    }

    public void SetMesh(MeshId meshId) => _meshId = meshId;
    public void SetSkyMaterial(MaterialId materialId) => _materialId = materialId;

    internal void GetDrawEntity(out DrawEntity drawEntity)
    {
        var mesh = new MeshComponent(_meshId, _materialId, 0);
        EntityUtility.MakeDrawTerrain(in mesh, in _transform, out drawEntity);
    }
}