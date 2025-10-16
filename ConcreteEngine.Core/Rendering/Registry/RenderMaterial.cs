#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

//internal delegate void MaterialApplyDel(in MaterialParams param);
/*
public sealed class RenderMaterial
{
    public MaterialId Id { get; }
    public ShaderId ShaderId { get; private set; }
    public bool Alive { get; internal set; } = true;

    private MaterialParams _matParams;

    private readonly TextureSlotInfo[] _samplerSlots;

    internal RenderMaterial(MaterialId id, ShaderId shader, in MaterialParams param, ReadOnlySpan<TextureSlotInfo> slots)
    {
        Id = id;
        ShaderId = shader;
        _samplerSlots = slots.ToArray();
        _matParams = param;
    }

    public ReadOnlySpan<TextureSlotInfo> SamplerSlots => _samplerSlots;
    public ref readonly MaterialParams MaterialParams => ref _matParams;

    public void SetMaterialParams(in MaterialParams param) => _matParams = param;
}
*/