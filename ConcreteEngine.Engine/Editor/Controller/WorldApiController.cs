using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Shared.RenderData;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx)
{
    public long FillCameraData(long version, ref CameraEditorPayload data)
    {
        var camera = ctx.World.Camera;
        if (camera.Generation == version) return camera.Generation;

        data.Generation = camera.Generation;
        data.ViewTransform = new ViewTransformData(camera.Translation, camera.Scale, camera.Orientation);
        data.Projection =
            new ProjectionInfoData(camera.AspectRatio, camera.Fov, camera.NearPlane, camera.FarPlane);
        data.Viewport = camera.Viewport;
        return camera.Generation;
    }
    
    
    public long WriteCameraData(long version, ref CameraEditorPayload data)
    {
        var camera = ctx.World.Camera;

        if (camera.Generation == version) return version;
        WorldActionSlot.SetSlot(version, in data);
        return camera.Generation;
    }
    
    public long FillWorldParams(long version, ref WorldParamState data)
    {
        var snapshot = ctx.World.WorldRenderParams.Snapshot;
        if (version == snapshot.Version) return version;

        data.LightState.DirectionalLight = new DirLightState(in snapshot.DirLight);
        data.LightState.AmbientLight = new AmbientState(in snapshot.Ambient);
        data.FogState = new FogState(in snapshot.Fog);
        data.PostState.Grade = new PostGradeState(in snapshot.PostEffects.Grade);
        data.PostState.WhiteBalance = new PostWhiteBalanceState(snapshot.PostEffects.WhiteBalance);
        data.PostState.Bloom = new PostBloomState(in snapshot.PostEffects.Bloom);
        data.PostState.ImageFx = new PostImageFxState(in snapshot.PostEffects.ImageFx);

        return snapshot.Version;
    }

    public long WriteWorldParams(long version, ref WorldParamState data)
    {
        var snapshot = ctx.World.WorldRenderParams.Snapshot;
        if (version == snapshot.Version) return snapshot.Version;

        ref var slot = ref WorldActionSlot.WriteSlot<WorldParamsData>(version);

        slot.DirLight = Unsafe.As<DirLightState, DirLightParams>(ref data.LightState.DirectionalLight);
        slot.Ambient = Unsafe.As<AmbientState, AmbientParams>(ref data.LightState.AmbientLight);
        slot.Fog = Unsafe.As<FogState, FogParams>(ref data.FogState);
        slot.PostEffect = Unsafe.As<PostEffectState, PostEffectParams>(ref data.PostState);
        return snapshot.Version;
    }

}