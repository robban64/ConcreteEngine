#region

#endregion

namespace ConcreteEngine.Core.Rendering;

public static class RenderConsts
{
    public const int MaxSpriteBatchSize = 1024;

    public static readonly int RenderTargetCount = Enum.GetValues<RenderTargetId>().Length;
}