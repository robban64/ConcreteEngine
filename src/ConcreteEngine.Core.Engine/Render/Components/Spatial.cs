using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;


public struct WorldTransform : IRenderComponent<WorldTransform>
{
    public Matrix4x4 Transform;
}

public struct NormalMatrix : IRenderComponent<NormalMatrix>
{
    public Matrix3X4 Normal;
}

public struct WorldBox : IRenderComponent<WorldBox>
{
    public Vector3 Center;
    public float Pad16;
    public Vector3 Extent;
    public float Pad32;

    public WorldBox()
    {
        Center = Vector3.Zero;
        Extent = Vector3.Zero;
        Pad16 = 0;
        Pad32 = 1;
        
    }

    public WorldBox(in BoundingBox box)
    {
        Center = box.Center;
        Extent = box.Extent;
        Pad16 = 0;
        Pad32 = 1;
    }
    
    public WorldBox(in BoundingAxisBox box)
    {
        Center = box.Center;
        Extent = box.Extent;
        Pad16 = 0;
        Pad32 = 1;
    }
    
    public readonly Vector3 Min
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Center - Extent;
    }

    public readonly Vector3 Max
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Center + Extent;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Fill(out BoundingAxisBox bounds) => bounds = new BoundingAxisBox(Center, Extent);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector256<float> AsVector256() => Unsafe.As<WorldBox, Vector256<float>>(ref Unsafe.AsRef(in this));

}


