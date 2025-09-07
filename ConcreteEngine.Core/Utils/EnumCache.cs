using ConcreteEngine.Core.Rendering;

namespace ConcreteEngine.Core.Utils;

internal static class EnumCache
{
    public static readonly RenderTargetId[] RenderTargetVals = Enum.GetValues<RenderTargetId>();
    public static readonly DrawCommandId[] DrawCommandVals = Enum.GetValues<DrawCommandId>();

}