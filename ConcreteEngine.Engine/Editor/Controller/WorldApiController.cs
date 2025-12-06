#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Shared.RenderData;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx)
{
    public void ProcessCameraRequest(ref EditorDataRequest<CameraDataState> request)
    {
        var camera = ctx.World.Camera;
        if (request.WriteRequest && request.Generation >= camera.Generation)
        {
            request.Generation = camera.Generation + 1;
            WorldActionSlot.SetSlot(request.Generation, in request.EditorData);
            request.ResponseStatus = EditorDataRequestStatus.Success;
            return;
        }

        ref var data = ref request.EditorData;
        request.Generation = camera.Generation;
        request.ResponseStatus = !request.WriteRequest ? EditorDataRequestStatus.Success : EditorDataRequestStatus.Overwrite;
        camera.FillEditorData(out data);
    }

    public void ProcessWorldParamsRequest(ref EditorDataRequest<WorldParamsData> request)
    {
        var renderParams = ctx.World.WorldRenderParams;
        if (request.WriteRequest && request.Generation >= renderParams.Version)
        {
            request.Generation = renderParams.Version + 1;
            WorldActionSlot.SetSlot(request.Generation, in request.EditorData);
            request.ResponseStatus = EditorDataRequestStatus.Success;
            return;
        }

        ref var data = ref request.EditorData;
        renderParams.FillEditorData(out data);
        request.Generation = renderParams.Version;
        request.ResponseStatus = !request.WriteRequest ? EditorDataRequestStatus.Success : EditorDataRequestStatus.Overwrite;
    }
}