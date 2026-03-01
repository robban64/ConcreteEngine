using System.Runtime.CompilerServices;

namespace ConcreteEngine.Editor.Utils;

internal struct RefreshRateTicker
{
    private const float RateIdle = 1f / 40f; //40Hz
    private const float RateActive = 1f / 60f; //60Hz
    private const float ActivityCooldown = 2.0f;

    private float _accumulator;
    private float _activityTimer;
    private float _currentStepSize;

    public static RefreshRateTicker Make()
    {
        var ticker = new RefreshRateTicker { _currentStepSize = RateIdle };
        return ticker;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Accumulate(float delta, out float step)
    {
        _accumulator += delta;
        if (_activityTimer > 0f) _activityTimer -= delta;
        if (_activityTimer <= 0f) _currentStepSize = RateIdle;

        if (_accumulator >= _currentStepSize)
        {
            _accumulator -= _currentStepSize;
            step = _currentStepSize;
            return true;
        }

        step = 0f;
        return false;
    }

    public void WakeUp()
    {
        _activityTimer = ActivityCooldown;
        _currentStepSize = RateActive;
    }
}