#region

#endregion

#region

using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering;

public static class RenderConsts
{
    public const int MaxSpriteBatchSize = 1024;

    public static int RenderTargetCount => EnumCache.RenderTargetVals.Length;
}