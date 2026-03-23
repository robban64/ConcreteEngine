using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Configuration.Setup;

namespace ConcreteEngine.Engine.Configuration;

internal sealed class EngineSetupPipeline
{
    private const int StepCount = 9;
    private readonly EngineSetupStep[] _steps = new EngineSetupStep[StepCount];
    public EngineSetupState CurrentStep = EngineSetupState.NotStarted;

    public float Progress => (float)CurrentStep / _steps.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Teardown()
    {
        Array.Clear(_steps);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RegisterStep<TCtx>(EngineSetupState state, TCtx ctx, Func<float, TCtx, bool> action) where TCtx : class
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)state, StepCount, nameof(state));
        if (_steps[(int)state] != null)
            throw new InvalidOperationException($"{state.ToString()} already registered");

        _steps[(int)state] = new EngineSetupStep<TCtx>(state, ctx, action);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RegisterRunner<TCtx>(EngineSetupState state, int frameWindow, TCtx ctx, Func<float, TCtx, bool> action)
        where TCtx : class
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameWindow);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)state, StepCount, nameof(state));
        if (_steps[(int)state] != null)
            throw new InvalidOperationException($"{state.ToString()} already registered");

        _steps[(int)state] = new EngineSetupStepRunner<TCtx>(state, frameWindow, ctx, action);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Run(float dt)
    {
        if (CurrentStep >= EngineSetupState.Running) return true;
        var step = _steps[(int)CurrentStep];
        if (step == null!)
        {
            CurrentStep++;
            return false;
        }

        bool isStepDone = step.Execute(dt);

        if (isStepDone) CurrentStep++;

        return CurrentStep >= EngineSetupState.Running;
    }

    private sealed class EngineSetupStep<TCtx>(EngineSetupState state, TCtx ctx, Func<float, TCtx, bool> action)
        : EngineSetupStep(state) where TCtx : class
    {
        protected override bool OnExecute(float dt) => action(dt, ctx);
    }

    private sealed class EngineSetupStepRunner<TCtx>(
        EngineSetupState state,
        int frameWindow,
        TCtx ctx,
        Func<float, TCtx, bool> action)
        : EngineSetupStep(state) where TCtx : class
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override bool OnExecute(float dt)
        {
            if (FramesExecuted >= frameWindow) return true;
            return action(dt, ctx);
        }
    }

    private abstract class EngineSetupStep(EngineSetupState state)
    {
        private const int MaxFrames = 1440; // sanity check

        private long _startTimestamp;

        public readonly EngineSetupState State = state;
        public int FramesExecuted { get; protected set; }
        public double DurationMs { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnEnter() => _startTimestamp = Stopwatch.GetTimestamp();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnLeave() => DurationMs = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;

        protected abstract bool OnExecute(float dt);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Execute(float dt)
        {
            if (FramesExecuted == 0) OnEnter();

            bool isStepDone = OnExecute(dt);
            if (isStepDone) OnLeave();

            if (FramesExecuted++ > MaxFrames)
            {
                throw new InvalidOperationException(
                    $"[Setup Deadlock] Stuck at '{State}' for {FramesExecuted} frames.");
            }

            return isStepDone;
        }
    }
}