namespace ConcreteEngine.Core.Engine.Assets.Data;

public enum MaterialProfile : byte
{
    None = 0,
    StaticModel = 1,
    AnimatedModel = 2,
    Terrain = 3,

    Sky = 4,
    Water = 5,

    Particle = 6,
    Foliage = 7
}