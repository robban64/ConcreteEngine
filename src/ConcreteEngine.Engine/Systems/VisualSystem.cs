using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Systems;

internal sealed unsafe class VisualSystem
{
    public static void Create(GfxBuffers gfx) => Instance = new VisualSystem(gfx);
    public static VisualSystem Instance { get; private set; } = null!;
    private static CameraManager CameraManager => CameraManager.Instance;
    private static VisualManager VisualManager => VisualManager.Instance;

    private readonly GfxBuffers _gfx;

    private long _sunVersion, _shadowVersion;
    private long _fogVersion, _ambientVersion;
    private long _postFxVersion;

    private VisualSystem(GfxBuffers gfx)
    {
        _gfx = gfx;
    }

    public void UploadUniformBuffers(RenderResolver resolver, MaterialSystem materialSystem,
        AnimationSystem animationSystem)
    {
        // Ensure ubo size
        var drawCount = IntMath.AlignUp(resolver.VisibleCount, 64);
        var materialCount = IntMath.AlignUp(materialSystem.Count, 16);
        var boneCount = IntMath.AlignUp(animationSystem.BoneCount, 64);

        if (!GfxRegistry.GetMeta(TransformUniform.UboId).HasCapacity(drawCount))
            _gfx.SetUniformBufferCount(TransformUniform.UboId, drawCount);

        if (!GfxRegistry.GetMeta(MaterialUniform.UboId).HasCapacity(materialCount))
            _gfx.SetUniformBufferCount(MaterialUniform.UboId, materialCount);

        if (!GfxRegistry.GetMeta(SkinningUniform.UboId).HasCapacity(boneCount))
            _gfx.SetUniformBufferCount(SkinningUniform.UboId, boneCount);

        var transforms = resolver.Transforms;
        if (transforms.Length > 0) _gfx.UploadUniform(transforms, 0);

        var materials = materialSystem.GetUniforms();
        if (materials.Length > 0) _gfx.UploadUniform(materials, 0);

        var boneData = animationSystem.GetUniforms();
        if (boneData.Length > 0) _gfx.UploadUniform(boneData, 0);
    }

    private void UploadDirtyUniforms()
    {
    }

    public void UploadUniforms()
    {
        UploadEngineUniform();
        UploadMainView();
        UploadShadow();

        var visualManager = VisualManager;
        if (!VisualManager.AnyWasDirty) return;

        if (visualManager.Lightning.Sun.Version != _sunVersion)
        {
            _sunVersion = visualManager.Lightning.Sun.Version;
            UploadLightningUniform();
        }

        if (visualManager.Environment.FogSettings.Version != _fogVersion ||
            visualManager.Environment.Ambient.Version != _ambientVersion)
        {
            _fogVersion = visualManager.PostEffect.Version;
            UploadEnvironmentUniform();
        }

        if (visualManager.PostEffect.Version != _postFxVersion)
        {
            _postFxVersion = visualManager.PostEffect.Version;
            UploadPostUniform();
        }

        visualManager.ClearWasDirty();
    }

    public void UploadPointLight()
    {
        PointLightUniform data = default;
        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    public void UploadMainView()
    {
        var t = CameraManager.FrameTransforms;
        CameraUniform data;
        data.ViewMatrix = t.ViewMatrix;
        data.ProjectionMatrix = t.ProjectionMatrix;
        data.ProjectionViewMatrix = t.ProjectionViewMatrix;
        data.CameraPos = t.Translation;
        data.CameraUp = t.Up;
        data.CameraRight = t.Right;
        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    public void UploadLightView()
    {
        var t = CameraManager.LightTransforms;
        CameraUniform data;
        data.ViewMatrix = t.ViewMatrix;
        data.ProjectionMatrix = t.ProjectionMatrix;
        data.ProjectionViewMatrix = t.ProjectionViewMatrix;
        data.CameraPos = t.Translation;
        data.CameraUp = t.Up;
        data.CameraRight = t.Right;
        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    public void UploadShadow()
    {
        ShadowUniform data;
        data.LightViewProjectionMatrix = CameraManager.LightTransforms.ProjectionViewMatrix;

        var shadow = VisualManager.Lightning.Shadow;
        var size = shadow.InvMapSize;
        data.ShadowParams0 = new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias);
        data.ShadowParams1 = new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, shadow.Distance);

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadEngineUniform()
    {
        var mouse = CoordinateMath.ToUvCoords(EngineInput.Mouse.ViewportPos, EngineWindow.ViewportSize);
        var data = new EngineUniformRecord(
            invResolution: EngineWindow.InvViewport,
            mouse: mouse,
            deltaTime: EngineTime.DeltaTimeF,
            time: EngineTime.TimeF,
            random: EngineTime.FrameRng
        );

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadEnvironmentUniform()
    {
        EnvironmentUniform data;

        var ambient = VisualManager.Environment.Ambient;
        data.Ambient = new Vector4(ambient.Ambient, ambient.Exposure);
        data.AmbientGround = new Vector4(ambient.AmbientGround, 0.0f);

        var fog = VisualManager.Environment.FogSettings;
        float kExp2 = 1f / (fog.Density * fog.Density);
        float kHeight = 1f / MathF.Max(fog.HeightFalloff, 1e-6f);
        data.FogColor = new Vector4(fog.FogColor, fog.Scattering);
        data.FogParams0 = new Vector4(kExp2, kHeight, fog.BaseHeight, fog.Strength);
        data.FogParams1 = new Vector4(fog.DistanceWeight, fog.HeightWeight, fog.MaxDistance, 0.0f);

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadLightningUniform()
    {
        LightningUniform data;

        var it = VisualManager.Lightning.Sun;
        data.Direction = it.Direction.AsVector4();
        data.Diffuse = new Vector4(it.Diffuse, it.Intensity);
        data.Specular = new Vector4(it.Specular, 0.0f, 0.0f, 0.0f);

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadPostUniform()
    {
        var post = VisualManager.PostEffect;
        var bloom = post.Bloom;
        var wb = post.WhiteBalance;

        PostFxUniform data;
        data.Grade = Unsafe.BitCast<PostGradeParams, Vector4>(post.Grade);
        data.WhiteBalance = new Vector4(wb.Tint, wb.Strength, 0f, 0f);
        data.Bloom = new Vector4(bloom.Intensity, bloom.Threshold, bloom.Radius, 0f);
        data.Fx = Unsafe.BitCast<PostImageFxParams, Vector4>(post.ImageFx);

        _gfx.UploadSingleUniform(&data, 0);
    }
}