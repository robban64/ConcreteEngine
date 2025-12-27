namespace ConcreteEngine.Engine.Assets.Materials;

public enum MaterialProfile : byte
{
    None = 0,
    StaticModel = 1,
    AnimatedModel = 2,
    Terrain = 3,

    Sky = 4,
    Water = 5,

    Particle = 6,
    Billboard = 7,
    Decal = 8,

    Ui = 9,
    Debug = 10
}