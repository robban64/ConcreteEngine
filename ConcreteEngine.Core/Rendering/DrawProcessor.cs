using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawProcessor
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly MaterialStore _materials;
    private readonly UniformBinder _uniformBinder;

    private int _previousMaterialId = -1;


    internal DrawProcessor(IGraphicsDevice graphics, MaterialStore materials, UniformBinder uniformBinder)
    {
        _graphics = graphics;
        _materials = materials;
        _uniformBinder = uniformBinder;
        _gfx = _graphics.Gfx;
    }


    public void Initialize(Render2D render2D, Render3D render3D)
    {
    }

    public void Prepare(in RenderGlobalSnapshot renderGlobals)
    {
        _previousMaterialId = -1;
    }

    private void BindMaterial(MaterialId materialId)
    {
        if (_previousMaterialId == materialId.Id) return;
        var material = _materials.GetMaterial(materialId);
        _gfx.UseShader(material.ShaderId);
        for (int t = 0; t < material.SamplerSlots.Length; t++)
        {
            _gfx.BindTexture(material.SamplerSlots[t], (uint)t);
        }

        _uniformBinder.UploadMaterial(new MaterialUniformRecord(materialId, material.Color.AsVec3(), material.Shininess,
            material.SpecularStrength, material.UvRepeat));

        _previousMaterialId = materialId.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadTransform(in DrawTransformPayload payload)
    {
        _uniformBinder.UploadDrawObject(in payload);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawMesh(in DrawCommand cmd)
    {
        BindMaterial(cmd.MaterialId);
        _uniformBinder.BindDrawObject();
        _gfx.BindMesh(cmd.MeshId);
        _gfx.DrawMesh(cmd.DrawCount);
    }
    

}