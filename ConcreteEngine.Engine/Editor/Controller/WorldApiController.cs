#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx)
{
    public long CameraGeneration => ctx.World.Camera.Generation;
    public long WorldParamGeneration => ctx.World.WorldRenderParams.Version;

    private World World => ctx.World;
    
    public void ApplyCameraState(in CameraDataState data) => World.Camera.SetFromData(in data);

    public void WriteCameraState(out CameraDataState data) => World.Camera.FillData(out data);

    public void ApplyWorldRenderParams(in WorldParamsData data) => World.WorldRenderParams.SetFromData(in data);

    public void WriteWorldRenderParams(out WorldParamsData data) => World.WorldRenderParams.FillData(out data);

}