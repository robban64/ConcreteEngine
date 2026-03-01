namespace ConcreteEngine.Core.Common;

public delegate void ActionIn<TIn>(in TIn input);

public delegate void FuncFill<TOut>(out TOut output);

public delegate TRet InFunc<TIn, out TRet>(in TIn input) where TIn : allows ref struct;