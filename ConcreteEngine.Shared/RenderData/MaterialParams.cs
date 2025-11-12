#region

using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Shared.RenderData;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialParams(Color4 color, float specular, float shininess, float uvRepeat = 1f)
{
    public readonly Color4 Color  = color;
    public readonly float Specular  = specular;
    public readonly float Shininess  = shininess;
    public readonly float UvRepeat  = uvRepeat;

}