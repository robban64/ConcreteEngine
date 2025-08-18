#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Rendering;

public static class RenderConsts
{
    public const int MaxMaterials = 16;
    public const int MaxSpriteBatchSize = 1024;

    public static readonly int DrawCommandTypeCount = Enum.GetValues<DrawCommandId>().Length;
    public static readonly int RenderTargetCount = Enum.GetValues<RenderTargetId>().Length;
}