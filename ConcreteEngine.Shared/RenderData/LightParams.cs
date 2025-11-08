#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Shared.RenderData;

public readonly record struct DirLightParams(Vector3 Direction, Vector3 Diffuse, float Intensity, float Specular);

public readonly record struct AmbientParams(Vector3 Ambient, Vector3 AmbientGround, float Exposure);

