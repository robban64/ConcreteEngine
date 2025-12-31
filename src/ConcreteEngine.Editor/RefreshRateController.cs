using System.Reflection;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Editor;
/*
internal sealed class RefreshRateController
{
    private const float RateIdle = 1f / 40f; //40Hz
    private const float RateActive = 1f / 60f; //60Hz
    private const float ActivityCooldown = 2.0f;

    private float _accumulator;
    private float _activityTimer;
    private float _currentStepSize = RateIdle;

    private bool _hasRenderedOnce;

    private ImDrawDataPtr _lastDrawData;

    private readonly Action<ImDrawDataPtr> _drawBinding;

    public RefreshRateController(ImGuiController controller)
    {
        var methodInfo = controller.GetType().GetMethod("RenderImDrawData",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (methodInfo == null)
            throw new Exception("Could not find RenderImDrawData. New update?");

        _drawBinding =
            (Action<ImDrawDataPtr>)Delegate.CreateDelegate(typeof(Action<ImDrawDataPtr>), controller, methodInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Draw()
    {
        if (!_hasRenderedOnce) return;
        _drawBinding(_lastDrawData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDelta(float delta)
    {
        _accumulator += delta;
        if (_activityTimer > 0f)
        {
            _activityTimer -= delta;
            if (_activityTimer <= 0f) _currentStepSize = RateIdle;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldUpdate(out float step)
    {
        if (_accumulator >= _currentStepSize)
        {
            _accumulator -= _currentStepSize;
            step = _currentStepSize;
            return true;
        }

        step = 0f;
        return false;
    }

    public void EndUpdate()
    {
        _lastDrawData = ImGui.GetDrawData();
        _hasRenderedOnce = true;
    }

    public void WakeUp()
    {
        _activityTimer = ActivityCooldown;
        _currentStepSize = RateActive;
    }
}*/