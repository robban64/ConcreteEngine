using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Rendering.Commands;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawMaterialCommand(MaterialId materialId)
{
    public readonly MaterialId MaterialId = materialId;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawMaterialCommandRef : IComparable<DrawMaterialCommandRef>
{
    public readonly int SubmitIdx;
    public readonly MaterialId MaterialId;
    public readonly ShaderId ShaderId;

    public DrawMaterialCommandRef(int submitIdx, MaterialId materialId, ShaderId shaderId)
    {
        SubmitIdx = submitIdx;
        MaterialId = materialId;
        ShaderId = shaderId;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawMaterialCommandRef(DrawMaterialCommand cmd, int  submitIdx)
    {
        SubmitIdx = submitIdx;
        MaterialId = cmd.MaterialId;
    }


    public int CompareTo(DrawMaterialCommandRef other)
    {
        if (ShaderId != other.ShaderId) return ShaderId < other.ShaderId ? -1 : 1;
        if (SubmitIdx != other.SubmitIdx) return SubmitIdx < other.SubmitIdx ? -1 : 1;
        return 0;
    }
}