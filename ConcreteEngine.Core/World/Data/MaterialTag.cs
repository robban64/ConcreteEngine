using System.Runtime.InteropServices;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Core.World.Data;



public readonly record struct MaterialTagKey(int Value);

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct MaterialTag
{
    public readonly MaterialId Slot0;
    public MaterialId Slot1 { get; init; }
    public MaterialId Slot2 { get; init; }
    public MaterialId Slot3 { get; init; }
    public MaterialId Slot4 { get; init; }
    public MaterialId Slot5 { get; init; }
    public MaterialId Slot6 { get; init; }
    public int Count { get; init; }

    public MaterialTag(MaterialId slot0, MaterialId slot1 = default, MaterialId slot2 = default,
        MaterialId slot3 = default, MaterialId slot4 = default,
        MaterialId slot5 = default, MaterialId slot6 = default)
    {
        Slot0 = slot0;
        Slot1 = slot1;
        Slot2 = slot2;
        Slot3 = slot3;
        Slot4 = slot4;
        Slot5 = slot5;
        Slot6 = slot6;

        var c = 7;
        if (Slot0 == 0) c = 0;
        else if (Slot1 == 0) c = 1;
        else if (Slot2 == 0) c = 2;
        else if (Slot3 == 0) c = 3;
        else if (Slot4 == 0) c = 4;
        else if (Slot5 == 0) c = 5;
        else if (Slot6 == 0) c = 6;

        Count = c;
    }
}