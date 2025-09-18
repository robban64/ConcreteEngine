namespace ConcreteEngine.Common;

public interface IBuilderState
{
}

public interface IBuilds<out TResult>
{
    TResult Build();
}

public sealed class CommonBuilder<TBuilder, TResult, TState>
    where TBuilder : CommonBuilderBase<TResult, TState>
    where TState : IBuilderState
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

    /// Uniform entry point everywhere.
    public TBuilder CreateBuilder()
    {
        _builder.BeginSession(_stateFactory());
        return _builder;
    }
    
    public TProduct Build()
    {
        if (!_initialized)
            throw new InvalidOperationException("Use CreateBuilder() on the manager before Build().");

        ValidateCore(State);               // builder-led validation (to be added later)
        var product = ConstructCore(State);
        _initialized = false;              // close session, prevent stale reuse
        return product;
    }

}

public abstract class CommonBuilderBase<TResult, TState> : IBuilds<TResult>
    where TState : IBuilderState
{
    private bool _initialized;
    protected TState State { get; private set; } = default!;

    public CommonBuilderBase<TResult, TState> With(Action action)
    {
        action();
        return this;
    }


    internal void BeginSession(TState freshState)
    {
        ArgumentNullException.ThrowIfNull(freshState, nameof(freshState));
        State = freshState;
        ResetBuilder(State);
        _initialized = true;
    }

    public TResult Build()
    {
        InvalidOpThrower.ThrowIfFalse(_initialized, nameof(_initialized));

        ValidateBuilder(State);

        var product = BuildResult(State);
        _initialized = false;
        return product;
    }


    protected abstract void ResetBuilder(TState state);

    protected virtual void ValidateBuilder(TState state)
    {
    }

    protected abstract TResult BuildResult(TState state);
}