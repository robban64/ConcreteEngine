#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

//internal delegate void MaterialApplyDel(in MaterialParams param);

public sealed class RenderMaterial
{
    public MaterialId Id { get; }
    public ShaderId ShaderId { get; private set; }

    private readonly TextureId[] _samplerSlots;

    private MaterialParams _matParams;

    internal RenderMaterial(MaterialId id, ShaderId shader, ReadOnlySpan<TextureId> slots, in MaterialParams param)
    {
        Id = id;
        ShaderId = shader;
        _samplerSlots = slots.ToArray();
        _matParams = param;
    }

    public ReadOnlySpan<TextureId> SamplerSlots => _samplerSlots;
    public ref readonly MaterialParams MaterialParams => ref _matParams;

    public void SetMaterialParams(in MaterialParams param) => _matParams = param;
}