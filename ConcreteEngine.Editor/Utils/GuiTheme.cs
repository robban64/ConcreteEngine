#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Editor.Utils;

internal static class GuiTheme
{
    public static Vector4 ConsoleBgColor = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
    public static Vector4 ConsoleInnerBgColor = new Vector4(0.10f, 0.10f, 0.10f, 0.75f);
    public static int LeftSidebarWidth = 268;
    
    public static int RightSidebarCompactWidth = 160;
    public static int RightSidebarExpandedWidth = 268;
    public static bool RightSidebarExpanded { get; set; } = false;
    public static int RightSidebarWidth => RightSidebarExpanded ? RightSidebarExpandedWidth : RightSidebarCompactWidth;

    public static int TopbarHeight => 44;
    public static float PanelOpacity => 0.95f;

    public static readonly Vector4 PrimaryColor = Color4.FromRgba(0, 121, 193).AsVec4();
    public static readonly Vector4 SelectedColor = Color4.FromRgba(46, 163, 242).AsVec4();

    public static readonly Vector4 Blue1 = Color4.FromRgba(77, 174, 225).AsVec4();
    public static readonly Vector4 Blue2 = Color4.FromRgba(128, 195, 233).AsVec4();


    //public static readonly Vector4 BlueSecondary = Color4.FromRgba(33, 116, 166).AsVec4();
}