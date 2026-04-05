using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Engine.Platform;

namespace ConcreteEngine.Engine.Configuration;

internal sealed class EngineSetupPipeline
{
    private const int StepCount = 9;
    private EngineSetupStep[] _steps = new EngineSetupStep[StepCount];
    public EngineSetupState CurrentStep = EngineSetupState.NotStarted;
    private EngineSetupCtx _ctx;

    public float Progress => (float)CurrentStep / _steps.Length;

    internal static EngineSetupPipeline? Instance;

    private EngineSetupPipeline(EngineSetupCtx ctx) => _ctx = ctx;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Setup(EngineSetupCtx ctx)
    {
        if (Instance != null) throw new InvalidOperationException();
        Instance = new EngineSetupPipeline(ctx);
        EngineSetupBootstrapper.RegisterSteps(Instance, ctx);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Teardown()
    {
        EngineHost.IsSetup = false;
        EngineHost.IsSetupSimulation = false;

        _ctx.InputSystem.ClearInputState();
        _ctx.TickHub.Reset();

        _ctx.Renderer.BeforeUpdate(_ctx.Window.OutputSize);
        _ctx.Renderer.AfterUpdate();

        Array.Clear(_steps);
        Instance = null;
        _steps = null!;
        _ctx = null!;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Run()
    {
        if (CurrentStep >= EngineSetupState.Running) return true;
        var step = _steps[(int)CurrentStep];
        if (step == null!)
        {
            CurrentStep++;
            return false;
        }

        bool isStepDone = step.Execute();

        if (isStepDone) CurrentStep++;

        return CurrentStep >= EngineSetupState.Running;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RegisterStep<TCtx>(EngineSetupState state, TCtx ctx, Func< TCtx, bool> action) where TCtx : class
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)state, StepCount, nameof(state));
        if (_steps[(int)state] != null)
            throw new InvalidOperationException($"{state.ToString()} already registered");

        _steps[(int)state] = new EngineSetupStep<TCtx>(state, ctx, action);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RegisterRunner<TCtx>(EngineSetupState state, int frameWindow, TCtx ctx, Func<TCtx, bool> action)
        where TCtx : class
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameWindow);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)state, StepCount, nameof(state));
        if (_steps[(int)state] != null)
            throw new InvalidOperationException($"{state.ToString()} already registered");

        _steps[(int)state] = new EngineSetupStepRunner<TCtx>(state, frameWindow, ctx, action);
    }


    private sealed class EngineSetupStep<TCtx>(EngineSetupState state, TCtx ctx, Func<TCtx, bool> action)
        : EngineSetupStep(state) where TCtx : class
    {
        protected override bool OnExecute() => action(ctx);
    }

    private sealed class EngineSetupStepRunner<TCtx>(
        EngineSetupState state,
        int frameWindow,
        TCtx ctx,
        Func<TCtx, bool> action)
        : EngineSetupStep(state) where TCtx : class
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override bool OnExecute()
        {
            if (FramesExecuted >= frameWindow) return true;
            return action(ctx);
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

        protected abstract bool OnExecute();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Execute()
        {
            if (FramesExecuted == 0) OnEnter();

            var isStepDone = OnExecute();
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