namespace ConcreteEngine.Engine.Scene;

public readonly record struct SceneObjectId(int Value, ushort Gen)
{
    public int Index() => Value - 1;
    public static implicit operator int(SceneObjectId handle) => handle.Value;
}