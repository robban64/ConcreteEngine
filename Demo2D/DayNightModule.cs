#region

using ConcreteEngine.Common;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Rendering;

#endregion

namespace Demo2D;

public class DayNightModule : GameModule
{
    private IRenderSystem _renderer = null!;

    private readonly Color4 _dayColor = Color4.FromRgba(200, 200, 255);
    private readonly Color4 _nightColor = Color4.FromRgba(50, 50, 100);

    private Color4 _ambientColor = Color4.FromRgba(125, 125, 150);
    private Color4 _targetColor;

    private bool _towardsDay = true;
    private const float LerpSpeed = 0.02f;

    private List<float> _originalIntensity = [];

    public override void Initialize()
    {
        _targetColor = _dayColor;
    }

    public override void OnSceneReady()
    {
        _renderer = Context.GetSystem<IRenderSystem>();
    }

    public override void UpdateTick(int tick)
    {
        _ambientColor = Color4.Lerp(_ambientColor, _targetColor, LerpSpeed);
        var diff = (_ambientColor.AsVec3() - _targetColor.AsVec3()).LengthSquared();
        if (diff < 0.0001f)
        {
            _towardsDay = !_towardsDay;
            _targetColor = _towardsDay ? _dayColor : _nightColor;
        }

        _renderer.MutateRenderPass(RenderTargetId.SceneLight, new RenderPassMutation { ClearColor = _ambientColor });


        // Just a demo
        var lights = Context.World.Lights.AsSpan();
        if (_originalIntensity.Count == 0)
        {
            foreach (ref var light in lights)
                _originalIntensity.Add(light.Intensity);
        }

        if (!_towardsDay)
        {
            for (var i = 0; i < lights.Length; i++)
            {
                ref var light = ref lights[i];
                var originalIntensity = _originalIntensity[i];
                light.Intensity = float.Lerp(light.Intensity, originalIntensity, LerpSpeed);
            }
        }
        else
        {
            for (var i = 0; i < lights.Length; i++)
            {
                ref var light = ref lights[i];
                light.Intensity = float.Lerp(light.Intensity, 0.5f, LerpSpeed);
            }
        }
    }
}