namespace ConcreteEngine.Core;

public interface IGameFeature
{
    public bool IsUpdateable { get; }
    public int Order { get; }

    public void UpdateTick(int tick);
    public void Load(GameFeatureContext context, int order);
    public void Unload();
}

public interface IDrawableFeature : IGameFeature
{
    public bool IsDrawable { get; }
    public int DrawOrder { get; }
}

public interface IDrawableFeature<out T> : IDrawableFeature
{
    public T GetDrawables();
}
