using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Editor.UI;

public static class Palette
{
    public static readonly Color4 TextPrimary = new(0.90f, 0.90f, 0.90f);
    public static readonly Color4 TextSecondary = new(0.75f, 0.75f, 0.75f);
    public static readonly Color4 TextMuted = new(0.55f, 0.55f, 0.55f);
    public static readonly Color4 TextDisabled = new(0.40f, 0.40f, 0.40f);

    // theme
    public static readonly Color4 PrimaryColor = new(0.00f, 0.47f, 0.76f);
    public static readonly Color4 SelectedColor = new(0.18f, 0.64f, 0.95f);
    public static readonly Color4 HoverColor = new(0.3f, 0.68f, 0.88f);
    
    public static readonly Color4 ConsoleBgColor = new(0.08f, 0.08f, 0.08f, 0.94f);
    public static readonly Color4 ConsoleInnerBgColor = new(0.10f, 0.10f, 0.10f, 0.75f);


    // resources
    public static readonly Color4 Shader = new(0.392f, 0.584f, 0.929f);
    public static readonly Color4 Model = new(1f, 0.647f, 0f);
    public static readonly Color4 Texture = new(0.4f, 0.4f, 0.8f);
    public static readonly Color4 Material = new(0.4f, 0.8f, 0.4f);

    // generic
    public static readonly Color4 RedBase = new(0.94f, 0.33f, 0.31f);
    public static readonly Color4 RedLight = new(1.00f, 0.55f, 0.55f);
    public static readonly Color4 RedDark = new(0.60f, 0.15f, 0.15f);

    public static readonly Color4 OrangeBase = new(1.00f, 0.60f, 0.20f);
    public static readonly Color4 OrangeLight = new(1.00f, 0.75f, 0.45f);
    public static readonly Color4 OrangeDark = new(0.75f, 0.35f, 0.05f);

    public static readonly Color4 YellowBase = new(1.00f, 0.85f, 0.30f);
    public static readonly Color4 YellowLight = new(1.00f, 0.95f, 0.60f);
    public static readonly Color4 YellowDark = new(0.70f, 0.55f, 0.10f);

    public static readonly Color4 GreenBase = new(0.30f, 0.85f, 0.50f);
    public static readonly Color4 GreenLight = new(0.55f, 1.00f, 0.70f);
    public static readonly Color4 GreenDark = new(0.15f, 0.50f, 0.25f);

    public static readonly Color4 CyanBase = new(0.25f, 0.88f, 0.90f);
    public static readonly Color4 CyanLight = new(0.60f, 0.95f, 0.98f);
    public static readonly Color4 CyanDark = new(0.10f, 0.50f, 0.55f);

    public static readonly Color4 TealBase = new(0.00f, 0.65f, 0.65f);
    public static readonly Color4 TealLight = new(0.30f, 0.90f, 0.90f);
    public static readonly Color4 TealDark = new(0.00f, 0.40f, 0.40f);

    public static readonly Color4 BlueBase = new(0.30f, 0.60f, 1.00f);
    public static readonly Color4 BlueLight = new(0.60f, 0.80f, 1.00f);
    public static readonly Color4 BlueDark = new(0.10f, 0.30f, 0.65f);

    public static readonly Color4 PurpleBase = new(0.70f, 0.45f, 1.00f);
    public static readonly Color4 PurpleLight = new(0.85f, 0.70f, 1.00f);
    public static readonly Color4 PurpleDark = new(0.40f, 0.20f, 0.70f);

    public static readonly Color4 PinkBase = new(1.00f, 0.40f, 0.70f);
    public static readonly Color4 PinkLight = new(1.00f, 0.70f, 0.85f);
    public static readonly Color4 PinkDark = new(0.70f, 0.15f, 0.40f);

    public static readonly Color4 GrayBase = new(0.60f, 0.62f, 0.65f);
    public static readonly Color4 GrayLight = new(0.85f, 0.87f, 0.90f);
    public static readonly Color4 GrayDark = new(0.20f, 0.22f, 0.25f);
}