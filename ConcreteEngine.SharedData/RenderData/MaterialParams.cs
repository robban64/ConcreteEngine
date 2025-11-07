#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Shared.RenderData;

public readonly record struct MaterialParams(Color4 Color, float Specular, float Shininess, float UvRepeat = 1f);