#region

using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Shared.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct MaterialParameters
{
    public Color4 Color;
    public float Specular;
    public float SpecularFactor;
    public float Shininess;
    public float UvRepeat;
}