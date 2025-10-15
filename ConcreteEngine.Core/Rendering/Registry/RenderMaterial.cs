#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

//internal delegate void MaterialApplyDel(in MaterialParams param);


// wip
public sealed class RenderMaterial
{
    public MaterialId Id { get; }
    public ShaderId ShaderId { get; set; }

    private readonly TextureId[] _samplerSlots;

    private MaterialParams _materialParams;
    
    internal RenderMaterial(MaterialId id, ShaderId shaderId, int samplerSlots)
    {
        Id = id;
        ShaderId = shaderId;
        _samplerSlots = new TextureId[samplerSlots];
    }

    public ReadOnlySpan<TextureId>  SamplerSlots => _samplerSlots;
    public ref readonly  MaterialParams MaterialParams => ref _materialParams;

    public void SetMaterialParams(in MaterialParams param) => _materialParams = param;

}