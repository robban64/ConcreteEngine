using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;
// @formatter:off


[Inspect]
public sealed class PostEffectSettings : VisualStateObject
{
    [InspectInclude]
    public PostGradeParams Grade
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(1.0f, 1.1f, 1.05f, 0.0f);

    [InspectInclude]
    public PostWhiteBalanceParams WhiteBalance
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.0f, 0.0f);

    [InspectInclude]
    public PostBloomParams Bloom
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.5f, 0.85f, 3.0f);

    [InspectInclude]
    public PostImageFxParams ImageFx
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.25f, 0.15f, 0.20f, 0.0f);
}

