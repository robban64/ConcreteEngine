using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(Texture))]
internal sealed partial class TextureInspector : Inspector<TextureInspector>
{
    public static Texture Target => (Texture)SelectionManager.Instance.SelectedAsset!;

    public void Draw()
    {
    }
}
