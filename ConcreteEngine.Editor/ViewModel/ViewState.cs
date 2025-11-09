namespace ConcreteEngine.Editor.ViewModel;

internal sealed class ViewState<T> where T : class
{
    public required ModelState<T> ModelState { get; init; }
    
}