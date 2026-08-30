namespace ConcreteEngine.Core.Common.Identity;

public interface ITypedId<T> where T : ITypedId<T>
{
    int Index { get; }
    bool IsValid { get; }
}

public interface ITypedHandle<T> : ITypedId<T> where T : ITypedHandle<T>
{
    int Gen { get; }

    static abstract ulong Pack(T handle);
    static abstract T Unpack(ulong packedHandle);

}