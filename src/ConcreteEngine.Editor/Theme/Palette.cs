using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Editor.Theme;

public static class Palette32
{
    public const uint White = 0XFFFFFFFF;
    public const uint TextPrimary = 0XFFE6E6E6;
    public const uint TextSecondary = 0XFFBFBFBF;
    public const uint TextMuted = 0XFF8C8C8C;
    public const uint TextDisabled = 0XFF666666;
    public const uint TextLightBlue = 0XFFE6CCB3;
    
    public const uint PrimaryColor = 0XFFC27800;
    public const uint SelectedColor = 0XFFF2A32E;
    public const uint HoverColor = 0XFFE0AD4D;

    public const uint BackgroundColor = 0XFF242424;

    public const uint GrayBase = 0XFF404040;
    public const uint GrayLight = 0XFF2E2E2E;

    public const uint OrangeBase = 0XFF3399FF;
    public const uint RedBase = 0XFF4F54F0;

    public const uint Shader = 0XFFED9564;
    public const uint Model = 0XFF00A5FF;
    public const uint Texture = 0XFFCC6666;
    public const uint Material = 0XFF66CC66;

}

public static class Palette
{
    public static Color4 TextPrimary => new(0.90f, 0.90f, 0.90f);
    public static Color4 TextSecondary => new(0.75f, 0.75f, 0.75f);
    public static Color4 TextMuted => new(0.55f, 0.55f, 0.55f);
    public static Color4 TextDisabled => new(0.40f, 0.40f, 0.40f);
    public static Color4 TextLightBlue => new(0.70f, 0.80f, 0.90f);

    // theme
    public static Color4 PrimaryColor => new(0.00f, 0.47f, 0.76f);
    public static Color4 ActiveColor => new(0.18f, 0.64f, 0.95f);
    public static Color4 HoverColor => new(0.3f, 0.68f, 0.88f);
    
    public static Color4 BgColor => new(0.14f, 0.14f, 0.14f);

    public static Color4 SurfaceLight => new(0.25f, 0.25f, 0.25f);
    public static Color4 SurfaceDark => new(0.18f, 0.18f, 0.18f);

    public static Color4 FrameBg => new(0.20f, 0.25f, 0.29f);
    public static Color4 FrameBgHovered => new(0.25f, 0.31f, 0.36f);
    public static Color4 FrameBgActive => new(0.28f, 0.35f, 0.41f);

    // resources
    public static Color4 Shader => new(0.392f, 0.584f, 0.929f);
    public static Color4 Model => new(1f, 0.647f, 0f);
    public static Color4 Texture => new(0.4f, 0.4f, 0.8f);
    public static Color4 Material => new(0.4f, 0.8f, 0.4f);

    // generic
    public static Color4 RedBase => new(0.94f, 0.33f, 0.31f);
    public static Color4 RedLight => new(1.00f, 0.55f, 0.55f);
    public static Color4 RedDark => new(0.60f, 0.15f, 0.15f);

    public static Color4 OrangeBase => new(1.00f, 0.60f, 0.20f);
    public static Color4 OrangeLight => new(1.00f, 0.75f, 0.45f);
    public static Color4 OrangeDark => new(0.75f, 0.35f, 0.05f);

    public static Color4 YellowBase => new(1.00f, 0.85f, 0.30f);
    public static Color4 YellowLight => new(1.00f, 0.95f, 0.60f);
    public static Color4 YellowDark => new(0.70f, 0.55f, 0.10f);

    public static Color4 GreenBase => new(0.30f, 0.85f, 0.50f);
    public static Color4 GreenLight => new(0.55f, 1.00f, 0.70f);
    public static Color4 GreenDark => new(0.15f, 0.50f, 0.25f);

    public static Color4 CyanBase => new(0.25f, 0.88f, 0.90f);
    public static Color4 CyanLight => new(0.60f, 0.95f, 0.98f);
    public static Color4 CyanDark => new(0.10f, 0.50f, 0.55f);

    public static Color4 TealBase => new(0.00f, 0.65f, 0.65f);
    public static Color4 TealLight => new(0.30f, 0.90f, 0.90f);
    public static Color4 TealDark => new(0.00f, 0.40f, 0.40f);

    public static Color4 BlueBase => new(0.30f, 0.60f, 1.00f);
    public static Color4 BlueLight => new(0.60f, 0.80f, 1.00f);
    public static Color4 BlueDark => new(0.10f, 0.30f, 0.65f);

    public static Color4 PurpleBase => new(0.70f, 0.45f, 1.00f);
    public static Color4 PurpleLight => new(0.85f, 0.70f, 1.00f);
    public static Color4 PurpleDark => new(0.40f, 0.20f, 0.70f);

    public static Color4 PinkBase => new(1.00f, 0.40f, 0.70f);
    public static Color4 PinkLight => new(1.00f, 0.70f, 0.85f);
    public static Color4 PinkDark => new(0.70f, 0.15f, 0.40f);

}