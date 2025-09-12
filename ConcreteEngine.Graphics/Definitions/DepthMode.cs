namespace ConcreteEngine.Graphics;

public enum DepthMode : byte
{
    Unset,
    Disabled,
    ReadOnlyLequal,
    WriteLequal,
    WriteLess
}