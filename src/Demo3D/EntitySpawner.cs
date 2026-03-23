using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine;

namespace Demo3D;

public sealed class ScenePlacement(string name, ModelBlueprint blueprint, float offset = 0f)
{
    public readonly string Name = name;
    public readonly ModelBlueprint Blueprint = blueprint;
    public readonly float Offset = offset;
}

public sealed class EntitySpawner(GameSceneContext ctx, float size = 256f, float margin = 4f)
{
    private int _genIdx;

    private void CreateOnTerrain(ScenePlacement sp, Vector3 p, Vector3? s = null, Quaternion? r = null)
    {
        var height = ctx.ActiveTerrain.GetSmoothHeight(p.X, p.Z) + p.Y;
        var scale = s.GetValueOrDefault(Vector3.One);
        var rotation = r.GetValueOrDefault(Quaternion.Identity);

        var transform = new Transform(p with { Y = height }, in scale, in rotation);
        ctx.SceneManager.CreateSceneObject(new SceneObjectTemplate
        {
            Name = $"{sp.Name}-{_genIdx++}", Transform = transform, Blueprints = { sp.Blueprint }
        });
    }


    public void PlaceGroundRocksBasic(
        int amount,
        ScenePlacement[] variants,
        float intensity = 1f,
        int? seed = null)
    {
        if (amount <= 0 || variants.Length == 0) return;
        var rng = new FastRandom((uint)(seed ?? 123456));
        intensity = Clamp01(intensity);
        var (xPrev, zPrev) = RandXz(ref rng);

        for (int i = 0; i < amount; i++)
        {
            var (rx, rz) = RandXz(ref rng);
            float bias = 0.6f * intensity;
            float x = float.Lerp(rx, xPrev, bias);
            float z = float.Lerp(rz, zPrev, bias);

            float s = 0.9f + rng.NextFloat() * 0.4f;
            var scale = new Vector3(s);
            var rot = Yaw(rng.NextFloat() * MathF.Tau);

            var sp = variants[Random.Shared.Next(variants.Length)];
            CreateOnTerrain(sp, new Vector3(x, sp.Offset, z), scale, rot);
            xPrev = x;
            zPrev = z;
        }
    }

    public void PlaceTreesBasic(
        int amount,
        ScenePlacement[] variants,
        float intensity = 1f,
        int? seed = null)
    {
        if (amount <= 0 || variants.Length == 0) return;
        var rng = new Random(seed ?? 666);
        intensity = Clamp01(intensity);

        var center = new Vector2(size * 0.5f, size * 0.5f);
        float min = margin, max = size - margin;
        float maxRadius = MathF.Min(center.X, center.Y) - margin;
        float tighten = 0.2f + 0.7f * intensity;

        for (int i = 0; i < amount; i++)
        {
            float a = (float)(rng.NextDouble() * MathF.Tau);
            float t = (float)rng.NextDouble();
            float r = MathF.Pow(t, 1f / (0.0001f + tighten)) * maxRadius;
            float x = float.Clamp(center.X + MathF.Cos(a) * r, min, max);
            float z = float.Clamp(center.Y + MathF.Sin(a) * r, min, max);

            float tScale = 0.9f + (float)rng.NextDouble() * 0.3f;
            var scale = new Vector3(tScale, tScale * 1.3f, tScale);
            var rot = Yaw((float)(rng.NextDouble() * MathF.Tau));

            var sp = variants[rng.Next(variants.Length)];
            CreateOnTerrain(sp, new Vector3(x, sp.Offset, z), scale, rot);
        }
    }

    public void PlacePropsRingBasic(
        int amount,
        ScenePlacement[] variants,
        float intensity = 1f,
        int? seed = null)
    {
        if (amount <= 0 || variants.Length == 0) return;
        var rng = new Random(seed ?? 777);
        intensity = Clamp01(intensity);

        var c = new Vector2(size * 0.5f, size * 0.5f);
        float min = margin, max = size - margin;

        float rCenter = 65f;
        float halfBand = float.Lerp(25f, 8f, intensity);
        float rMin = rCenter - halfBand;
        float rMax = rCenter + halfBand;

        for (int i = 0; i < amount; i++)
        {
            float a = (float)(rng.NextDouble() * MathF.Tau);
            float r = rMin + (float)rng.NextDouble() * (rMax - rMin);
            float x = float.Clamp(c.X + MathF.Cos(a) * r, min, max);
            float z = float.Clamp(c.Y + MathF.Sin(a) * r, min, max);

            float s = 0.95f + (float)rng.NextDouble() * 0.3f;
            var scale = new Vector3(s);
            var rot = Yaw((float)(rng.NextDouble() * MathF.Tau));

            var sp = variants[rng.Next(variants.Length)];
            CreateOnTerrain(sp, new Vector3(x, sp.Offset, z), scale, rot);
        }
    }

    private (float x, float z) RandXz(ref FastRandom rng)
    {
        float max = size - margin;
        float x = float.Lerp(margin, max, rng.NextFloat());
        float z = float.Lerp(margin, max, rng.NextFloat());
        return (x, z);
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    private static Quaternion Yaw(float radians) => Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians);
}