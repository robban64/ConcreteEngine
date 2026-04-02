using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Editor.Theme;

public static class Palette32
{
    public static readonly uint TextPrimary   = new Color4(0.90f, 0.90f, 0.90f).ToPackedRgba();
    public static readonly uint TextSecondary = new Color4(0.75f, 0.75f, 0.75f).ToPackedRgba();
    public static readonly uint TextMuted     = new Color4(0.55f, 0.55f, 0.55f).ToPackedRgba();
    public static readonly uint TextDisabled  = new Color4(0.40f, 0.40f, 0.40f).ToPackedRgba();
    public static readonly uint TextLightBlue = new Color4(0.70f, 0.80f, 0.90f).ToPackedRgba();
    public static readonly uint OrangeBase    = new Color4(1.00f, 0.60f, 0.20f).ToPackedRgba();
    public static readonly uint RedBase       = new Color4(0.94f, 0.33f, 0.31f).ToPackedRgba();


}
public static class Palette
{
    public static readonly Color4 TextPrimary = new Color4(0.90f, 0.90f, 0.90f);
    public static readonly Color4 TextSecondary = new Color4(0.75f, 0.75f, 0.75f);
    public static readonly Color4 TextMuted = new Color4(0.55f, 0.55f, 0.55f);
    public static readonly Color4 TextDisabled = new Color4(0.40f, 0.40f, 0.40f);
    public static readonly Color4 TextLightBlue = new Color4(0.70f, 0.80f, 0.90f);

    // theme
    public static readonly Color4 PrimaryColor = new Color4(0.00f, 0.47f, 0.76f);
    public static readonly Color4 SelectedColor = new Color4(0.18f, 0.64f, 0.95f);
    public static readonly Color4 HoverColor = new Color4(0.3f, 0.68f, 0.88f);

    // resources
    public static readonly Color4 Shader = new Color4(0.392f, 0.584f, 0.929f);
    public static readonly Color4 Model = new Color4(1f, 0.647f, 0f);
    public static readonly Color4 Texture = new Color4(0.4f, 0.4f, 0.8f);
    public static readonly Color4 Material = new Color4(0.4f, 0.8f, 0.4f);

    // generic
    public static readonly Color4 RedBase = new Color4(0.94f, 0.33f, 0.31f);
    public static readonly Color4 RedLight = new Color4(1.00f, 0.55f, 0.55f);
    public static readonly Color4 RedDark = new Color4(0.60f, 0.15f, 0.15f);

    public static readonly Color4 OrangeBase = new Color4(1.00f, 0.60f, 0.20f);
    public static readonly Color4 OrangeLight = new Color4(1.00f, 0.75f, 0.45f);
    public static readonly Color4 OrangeDark = new Color4(0.75f, 0.35f, 0.05f);

    public static readonly Color4 YellowBase = new Color4(1.00f, 0.85f, 0.30f);
    public static readonly Color4 YellowLight = new Color4(1.00f, 0.95f, 0.60f);
    public static readonly Color4 YellowDark = new Color4(0.70f, 0.55f, 0.10f);

    public static readonly Color4 GreenBase = new Color4(0.30f, 0.85f, 0.50f);
    public static readonly Color4 GreenLight = new Color4(0.55f, 1.00f, 0.70f);
    public static readonly Color4 GreenDark = new Color4(0.15f, 0.50f, 0.25f);

    public static readonly Color4 CyanBase = new Color4(0.25f, 0.88f, 0.90f);
    public static readonly Color4 CyanLight = new Color4(0.60f, 0.95f, 0.98f);
    public static readonly Color4 CyanDark = new Color4(0.10f, 0.50f, 0.55f);

    public static readonly Color4 TealBase = new Color4(0.00f, 0.65f, 0.65f);
    public static readonly Color4 TealLight = new Color4(0.30f, 0.90f, 0.90f);
    public static readonly Color4 TealDark = new Color4(0.00f, 0.40f, 0.40f);

    public static readonly Color4 BlueBase = new Color4(0.30f, 0.60f, 1.00f);
    public static readonly Color4 BlueLight = new Color4(0.60f, 0.80f, 1.00f);
    public static readonly Color4 BlueDark = new Color4(0.10f, 0.30f, 0.65f);

    public static readonly Color4 PurpleBase = new Color4(0.70f, 0.45f, 1.00f);
    public static readonly Color4 PurpleLight = new Color4(0.85f, 0.70f, 1.00f);
    public static readonly Color4 PurpleDark = new Color4(0.40f, 0.20f, 0.70f);

    public static readonly Color4 PinkBase = new Color4(1.00f, 0.40f, 0.70f);
    public static readonly Color4 PinkLight = new Color4(1.00f, 0.70f, 0.85f);
    public static readonly Color4 PinkDark = new Color4(0.70f, 0.15f, 0.40f);

    public static readonly Color4 GrayBase = new Color4(0.60f, 0.62f, 0.65f);
    public static readonly Color4 GrayLight = new Color4(0.85f, 0.87f, 0.90f);
    public static readonly Color4 GrayDark = new Color4(0.20f, 0.22f, 0.25f);
}