using ConcreteEngine.Renderer;

namespace ConcreteEngine.Engine.Assets.Materials;

public readonly struct MaterialMeta(
    MaterialId materialId,
    bool hasTransparency,
    bool hasNormal,
    bool hasAlpha,
    bool isAsset)
{
    public readonly MaterialId MaterialId = materialId;
    public readonly bool HasTransparency = hasTransparency;
    public readonly bool HasNormal = hasNormal;
    public readonly bool HasAlpha = hasAlpha;
    public readonly bool IsAsset = isAsset;
}