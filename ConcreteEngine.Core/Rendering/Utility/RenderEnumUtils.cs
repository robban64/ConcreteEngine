#region

using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Passes;

#endregion

namespace ConcreteEngine.Core.Rendering.Utility;

internal sealed class RenderEnumUtils
{
    public static PassMask FromPassBit(byte passId) => (PassMask)(1u << passId);
}