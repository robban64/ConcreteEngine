using System.Numerics;
using ConcreteEngine.Core.Game.Sprite;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Rendering.Pipeline;

namespace ConcreteEngine.Core.Game.Effects;

public struct LightEntity(Vector2 position, Vector3 color, float radius, float intensity)
{
    public Vector2 Position = position;
    public Vector3 Color = color;
    public float Radius = radius;
    public float Intensity = intensity;
    public Vector2 Delta = Vector2.Zero;

    public static DrawCommandLight ToCmd(in LightEntity entity)
    {
        return new DrawCommandLight(entity.Position, entity.Color, entity.Radius, entity.Intensity);
    }
}


public sealed class LightFeatureDrawData
{
    public List<LightEntity> Entities { get; set; }

}