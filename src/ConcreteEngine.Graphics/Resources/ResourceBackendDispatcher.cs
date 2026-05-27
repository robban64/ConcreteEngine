namespace ConcreteEngine.Graphics.Resources;

internal sealed class ResourceBackendDispatcher
{
    public required Action<DeleteResourceCommand> OnDelete { get; init; }
}