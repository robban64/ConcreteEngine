namespace ConcreteEngine.Core;

public interface IGameFeature
{
    public bool IsUpdateable { get; }
    public int Order { get; set; }

    public void UpdateTick(int tick);
    public void Load(GameFeatureContext context);
    public void Unload();
}