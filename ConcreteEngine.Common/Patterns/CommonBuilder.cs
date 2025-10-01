namespace ConcreteEngine.Common.Patterns;

public interface IBuilderState;

public interface IBuilds<out TResult>
{
    TResult Build();
}

public sealed class CommonBuilder<TBuilder, TResult, TState>
    where TBuilder : CommonBuilderBase<TResult, TState>
    where TState : class, IBuilderState
{
    private readonly TBuilder _builder;
    private readonly Func<TState> _stateFactory;

    public CommonBuilder(TBuilder builder, Func<TState> stateFactory)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(stateFactory, nameof(stateFactory));
        _builder = builder;
        _stateFactory = stateFactory;
    }

    public TBuilder CreateBuilder()
    {
        _builder.BeginSession(_stateFactory);
        return _builder;
    }

    public TResult Build()
    {
        return _builder.Build();
    }
}

public abstract class CommonBuilderBase<TResult, TState> : IBuilds<TResult> where TState : class, IBuilderState
{
    private bool _initialized;
    protected TState State { get; private set; } = null!;

    internal void BeginSession(Func<TState> stateFactory)
    {
        ArgumentNullException.ThrowIfNull(stateFactory, nameof(stateFactory));
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (State is null) State = stateFactory();
        ResetBuilder(State);
        _initialized = true;
        StartBuilder(State);
    }

    public TResult Build()
    {
        InvalidOpThrower.ThrowIfNot(_initialized, nameof(_initialized));
        if (State is null) throw new InvalidOperationException("State is null");

        ValidateBuilder(State);
        var product = BuildResult(State);
        _initialized = false;
        return product;
    }

    protected abstract void StartBuilder(TState state);

    protected abstract void ValidateBuilder(TState state);

    protected abstract TResult BuildResult(TState state);

    protected abstract void ResetBuilder(TState state);
}