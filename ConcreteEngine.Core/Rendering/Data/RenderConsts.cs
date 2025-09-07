#region

#endregion

using ConcreteEngine.Core.Utils;

namespace ConcreteEngine.Core.Rendering;

public static class RenderConsts
{
    public const int MaxSpriteBatchSize = 1024;

    public static int RenderTargetCount => EnumCache.RenderTargetVals.Length;
}