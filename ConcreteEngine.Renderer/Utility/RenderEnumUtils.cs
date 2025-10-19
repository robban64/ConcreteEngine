#region

using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Renderer.Utility;

internal sealed class RenderEnumUtils
{
    public static PassMask FromPassBit(byte passId) => (PassMask)(1u << passId);
}