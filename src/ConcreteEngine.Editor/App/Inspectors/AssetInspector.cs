using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Texture))]
internal sealed partial class TextureInspector : Inspector<TextureInspector>
{
    public static Texture Target => (Texture)SelectionManager.Instance.SelectedAsset.Asset;
}

[EditorInspector(typeof(Material))]
internal sealed partial class MaterialInspector : Inspector<MaterialInspector>
{
    public static Material Target => (Material)SelectionManager.Instance.SelectedAsset.Asset;

    private readonly ComboInput BlendCombo = ComboInput.Create("Blend Mode",
        BlendModeExt.Values,
        BlendModeExt.Names,
        static () => (int)Target.State.DrawFunctions.Blend,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with { Blend = (BlendMode)v });

    private readonly ComboInput CullCombo = ComboInput.Create("Cull Mode",
        CullModeExt.Values,
        CullModeExt.Names,
        static () => (int)Target.State.DrawFunctions.Cull,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with { Cull = (CullMode)v });

    private readonly ComboInput DepthCombo = ComboInput.Create("Depth Mode",
        DepthModeExt.Values,
        DepthModeExt.Names,
        static () => (int)Target.State.DrawFunctions.Depth,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with { Depth = (DepthMode)v });

    private readonly ComboInput PolygonCombo = ComboInput.Create("Polygon Offset",
        PolygonOffsetLevelExt.Values,
        PolygonOffsetLevelExt.Names,
        static () => (int)Target.State.DrawFunctions.PolygonOffset,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with
        {
            PolygonOffset = (PolygonOffsetLevel)v
        });

    public void DrawPipeline()
    {
        BlendCombo.Draw();
        CullCombo.Draw();
        DepthCombo.Draw();
        PolygonCombo.Draw();
    }
}