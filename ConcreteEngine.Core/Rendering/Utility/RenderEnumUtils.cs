using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Rendering.Commands;

namespace ConcreteEngine.Core.Rendering.Utility;

internal sealed class RenderEnumUtils
{
    public static PassMask FromPassBit(byte passId) => (PassMask)(1u << passId);
}