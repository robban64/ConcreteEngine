using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ImGuiNET;

namespace Core.DebugTools.Utils;

internal static class GuiTheme
{
    public static int TopbarHeight => 44;
    public static float PanelOpacity => 0.95f;

    public static readonly Vector4 PrimaryColor = Color4.FromRgba(0, 121, 193).AsVec4();
    public static readonly Vector4 HoverColor = Color4.FromRgba(46, 163, 242).AsVec4();

}