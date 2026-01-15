using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx) : WorldController
{
    private readonly World _world = ctx.World;
    private readonly Camera _camera = ctx.World.Camera;
    private readonly WorldVisual _worldVisual = ctx.World.WorldVisual;

    public override void CommitCamera(SlotView<EditorCameraState> slot)
    {
        if (slot.Gen != _camera.Generation)
        {
            _camera.FillData(ref slot.Data);
            slot.Gen = _camera.Generation;
            return;
        }

        _camera.SetFromData(in slot.Data);
    }


    public override void FetchCamera(SlotView<EditorCameraState> slot)
    {
        if (slot.Gen == _camera.Generation) return;
        _camera.FillData(ref slot.Data);
        slot.Gen = _camera.Generation;
    }


    public override void CommitVisualParams(SlotView<EditorVisualState> slot)
    {
        if (slot.Gen != _worldVisual.Generation)
        {
            _worldVisual.FillData(out slot.Data);
            slot.Gen = _worldVisual.Generation;
            return;
        }

        _worldVisual.SetFromData(in slot.Data);
    }

    public override void FetchVisualParams(SlotView<EditorVisualState> slot)
    {
        if (slot.Gen == _worldVisual.Generation) return;
        _worldVisual.FillData(out slot.Data);
        slot.Gen = _worldVisual.Generation;
    }
/*
    public List<EditorParticleResource> GetParticleEmitters()
    {
        var span = _world.Particles.EmitterSpan;
        List<EditorParticleResource> emitters = new(span.Length);
        foreach (var it in span)
        {
            emitters.Add(new EditorParticleResource
            {
                MeshId = it.Mesh,
                Id = it.EmitterHandle,
                Name = it.EmitterName,
                Generation = 1
            });
        }

        return emitters;
    }*/
}