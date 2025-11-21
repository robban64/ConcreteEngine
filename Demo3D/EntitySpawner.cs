#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Meshes;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace Demo3D;

public readonly record struct ScenePlacement(ModelBaseDrawInfo ModelInfo, in BoundingBox Bounds, MaterialTag Mat, float Offset = 0f);

public sealed class EntitySpawner(IWorld world, float size = 256f, float margin = 4f)
{
    private EntityId CreateOnTerrain(ScenePlacement sp, Vector3 p, Vector3? s = null, Quaternion? r = null)
    {
        var height = world.Terrain.GetSmoothHeight(p.X, p.Z) + p.Y;
        var scale = s.GetValueOrDefault(Vector3.One);
        var rotation = r.GetValueOrDefault(Quaternion.Identity);
        return CreateModelEntity(sp, new Transform(p with { Y = height }, scale, rotation));
    }

    private EntityId CreateModelEntity(ScenePlacement sp, Transform transform)
    {
        var entityId = world.Entities.Create();
        var key = world.EntityMaterials.Add(sp.Mat);
        world.Entities.Models.Add(entityId, new ModelComponent(sp.ModelInfo.Model, sp.ModelInfo.DrawCount, key));
        world.Entities.Transforms.Add(entityId, transform);
        world.Entities.BoundingBoxes.Add(entityId, new BoxComponent(sp.Bounds));

        return entityId;
    }

    public void PlaceGroundRocksBasic(
        int amount,
        ScenePlacement[] variants,
        float intensity = 1f,
        int? seed = null)
    {
        if (amount <= 0 || variants.Length == 0) return;
        var rng = new Random(seed ?? 123456);
        intensity = Clamp01(intensity);
        var (xPrev, zPrev) = RandXz(rng);

        for (int i = 0; i < amount; i++)
        {
            var (rx, rz) = RandXz(rng);
            float bias = 0.6f * intensity;
            float x = float.Lerp(rx, xPrev, bias);
            float z = float.Lerp(rz, zPrev, bias);

            float s = 0.9f + (float)rng.NextDouble() * 0.4f;
            var scale = new Vector3(s);
            var rot = Yaw((float)(rng.NextDouble() * MathF.Tau));

            var sp = variants[rng.Next(variants.Length)];
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

            ref readonly var sp = ref variants[rng.Next(variants.Length)];
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

    private (float x, float z) RandXz(Random rng)
    {
        float max = size - margin;
        float x = float.Lerp(margin, max, (float)rng.NextDouble());
        float z = float.Lerp(margin, max, (float)rng.NextDouble());
        return (x, z);
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    private static Quaternion Yaw(float radians) => Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians);
}