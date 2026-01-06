namespace ConcreteEngine.Core.Renderer.Material;

public readonly struct MaterialMeta(
    MaterialId materialId,
    bool hasTransparency,
    bool hasNormal,
    bool hasAlpha)
{
    public readonly MaterialId MaterialId = materialId;
    public readonly bool HasTransparency = hasTransparency;
    public readonly bool HasNormal = hasNormal;
    public readonly bool HasAlpha = hasAlpha;
}