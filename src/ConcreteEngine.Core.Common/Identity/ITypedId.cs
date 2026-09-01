namespace ConcreteEngine.Core.Common.Identity;

public interface ITypedId<T> where T : ITypedId<T>
{
    int Index { get; }
    bool IsValid { get; }
}
