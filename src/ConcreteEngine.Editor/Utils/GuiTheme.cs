using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Utils;

internal static class GuiTheme
{
    public static Vector4 ConsoleBgColor = new(0.08f, 0.08f, 0.08f, 0.94f);
    public static Vector4 ConsoleInnerBgColor = new(0.10f, 0.10f, 0.10f, 0.75f);

    public const int TopbarHeight = 44;
    public const float PanelOpacity = 0.95f;

    public const int LeftSidebarExpandedWidth = 264;
    public const int LeftSidebarCompactWidth = 220;

    public const int RightSidebarCompactWidth = 230;
    public const int RightSidebarExpandedWidth = 258; //248;


    public static int LeftSidebarWidth { get; private set; }
    public static int RightSidebarWidth { get; private set; }

    public static readonly Vector4 PrimaryColor = Color4.FromRgba(0, 121, 193);
    public static readonly Vector4 SelectedColor = Color4.FromRgba(46, 163, 242);

    public static readonly Vector4 Blue1 = Color4.FromRgba(77, 174, 225);
    public static readonly Vector4 Blue2 = Color4.FromRgba(128, 195, 233);

    //public static readonly Vector4 BlueSecondary = Color4.FromRgba(33, 116, 166);

    public static  void SetTheme(float scale)
    {
        var style = ImGui.GetStyle();
        var colors = style.Colors;
        
        style.ScaleAllSizes(1);

        style.TabRounding = 0.5f;
        style.TabBarBorderSize = 1f;
        style.TabBorderSize = 1f;

        style.ItemSpacing = new Vector2(8f, 6f);

        // Apply your rules
        colors[(int)ImGuiCol.Header]        = PrimaryColor;
        colors[(int)ImGuiCol.HeaderHovered] = Blue1;
        colors[(int)ImGuiCol.HeaderActive]  = SelectedColor;

        colors[(int)ImGuiCol.Tab]           = PrimaryColor;
        colors[(int)ImGuiCol.TabHovered]    = Blue1;
        colors[(int)ImGuiCol.TabSelected]   = SelectedColor;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushTheme(bool leftExpanded, bool rightExpanded)
    {
        LeftSidebarWidth = leftExpanded ? LeftSidebarExpandedWidth : LeftSidebarCompactWidth;
        RightSidebarWidth = rightExpanded ? RightSidebarExpandedWidth : RightSidebarCompactWidth;
    }

}