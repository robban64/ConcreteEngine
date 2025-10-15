#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

/*
public readonly record struct MaterialId(int Id)
{
    public static implicit operator int(MaterialId id) => id.Id;
    public static explicit operator MaterialId(int value) => new(value);
}
*/
internal sealed class RenderMaterialRegistry
{
    private readonly List<RenderMaterial> _materials = new(8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderMaterial GetMaterial(MaterialId id) => _materials[id.Id - 1];
}