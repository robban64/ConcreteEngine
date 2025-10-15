#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

//internal delegate void MaterialApplyDel(in MaterialParams param);

public readonly record struct MaterialParams(Color4 Color, float Specular, float Shininess, float UvRepeat, float Normal = 1f);

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

    public void SetParams(in MaterialParams param) => _materialParams = param;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<TextureId> GetData(out MaterialParams materialParams)
    {
        materialParams = _materialParams;
        return _samplerSlots;
    }
}