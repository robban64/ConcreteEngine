namespace ConcreteEngine.Core.Common;

public delegate void ActionIn<TIn>(in TIn input) where TIn : unmanaged;
public delegate void ActionInRet<TIn>(in TIn input) where TIn : unmanaged;

public delegate void FuncFill<TOut>(out TOut output) where TOut : unmanaged;

/*
public unsafe struct ActionRawPtr
{
    public unsafe delegate*<void*, void> FkPointer;
}

public unsafe struct ActionInPtr<TIn>(delegate*<in TIn, void> fkPointer)
    where TIn : unmanaged
{
    public delegate*<in TIn, void> FkPointer = fkPointer;
}

public unsafe struct FuncFillPtr<TOut>(delegate*<out TOut, void> fkPointer)
    where TOut : unmanaged
{
    public delegate*<out TOut, void> FkPointer = fkPointer;
}*/