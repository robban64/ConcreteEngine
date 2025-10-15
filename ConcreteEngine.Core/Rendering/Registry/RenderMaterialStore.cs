#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

/*
public readonly record struct MaterialId(int Id)
{
    public static implicit operator int(MaterialId id) => id.Id;
    public static explicit operator MaterialId(int value) => new(value);
}
*/
internal sealed class RenderMaterialStore
{
    private static int _idx = 0;
    private static MaterialId NextId() => new(++_idx);
    private static int CurrentSlot => _idx - 1;

    private RenderMaterial[] _materials = Array.Empty<RenderMaterial>();
    
    public int Count => _idx;

    internal RenderMaterialStore()
    {
    }
    
    public void CreateMaterial(ShaderId shader, ReadOnlySpan<TextureId> slots, in MaterialParams param)
    {
        var material = new RenderMaterial(NextId(), shader, slots, in param);
        _materials[CurrentSlot] = material;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RenderMaterial GetMaterial(MaterialId id) => _materials[id.Id - 1];

}