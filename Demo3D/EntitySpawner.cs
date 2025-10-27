using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using Silk.NET.Maths;

namespace Demo3D;

public readonly record struct ScenePlacement(Model Model, Material Material, float Offset = 0f);

public sealed class EntitySpawner(World world, float size = 256f, float margin = 4f)
{
    private EntityId CreateOnTerrain(Model model, Material mat, Vector3 p, Vector3? s = null, Quaternion? r = null)
    {
        var height = world.Terrain.GetSmoothHeight(p.X, p.Z) + p.Y;
        var scale = s.GetValueOrDefault(Vector3.One);
        var rotation = r.GetValueOrDefault(Quaternion.Identity);
        return CreateModelEntity(model, mat, new Transform(p with { Y = height }, scale, rotation));
    }

    private EntityId CreateModelEntity(Model model, Material material, Transform transform)
    {
        var entityId = world.Create();
        world.Meshes.Add(entityId, new ModelComponent(model.RenderId,  model.DrawCount));
        world.Transforms.Add(entityId, transform);
        world.EntityMaterials.AttachEntity(entityId, material.Id);

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
            float x = MathHelper.Lerp(rx, xPrev, bias);
            float z = MathHelper.Lerp(rz, zPrev, bias);

            var (mesh, mat,offset) = variants[rng.Next(variants.Length)];
            float s = 0.9f + (float)rng.NextDouble() * 0.4f;
            var scale = new Vector3(s);
            var rot = Yaw((float)(rng.NextDouble() * MathF.Tau));

            CreateOnTerrain(mesh, mat, new Vector3(x, offset, z), scale, rot);
            xPrev = x; zPrev = z;
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
        float maxRadius = (MathF.Min(center.X, center.Y) - margin);
        float tighten = 0.2f + 0.7f * intensity; 

        for (int i = 0; i < amount; i++)
        {
            float a = (float)(rng.NextDouble() * MathF.Tau);
            float t = (float)rng.NextDouble();
            float r = MathF.Pow(t, 1f / (0.0001f + tighten)) * maxRadius;
            float x = MathHelper.Clamp(center.X + MathF.Cos(a) * r, min, max);
            float z = MathHelper.Clamp(center.Y + MathF.Sin(a) * r, min, max);
            var (mesh, mat, offset) = variants[rng.Next(variants.Length)];
            float tScale = 0.9f + (float)rng.NextDouble() * 0.3f;
            var scale = new Vector3(tScale, tScale * 1.3f, tScale);
            var rot = Yaw((float)(rng.NextDouble() * MathF.Tau));
            CreateOnTerrain(mesh, mat, new Vector3(x, offset, z), scale, rot);
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
        float halfBand = MathHelper.Lerp(25f, 8f, intensity);
        float rMin = rCenter - halfBand;
        float rMax = rCenter + halfBand;

        for (int i = 0; i < amount; i++)
        {
            float a = (float)(rng.NextDouble() * MathF.Tau);
            float r = rMin + (float)rng.NextDouble() * (rMax - rMin);
            float x = MathHelper.Clamp(c.X + MathF.Cos(a) * r, min, max);
            float z = MathHelper.Clamp(c.Y + MathF.Sin(a) * r, min, max);

            var (mesh, mat, offset) = variants[rng.Next(variants.Length)];
            float s = 0.95f + (float)rng.NextDouble() * 0.3f;
            var scale = new Vector3(s);
            var rot = Yaw((float)(rng.NextDouble() * MathF.Tau));
            CreateOnTerrain(mesh, mat, new Vector3(x, offset, z), scale, rot);
        }
    }

    private (float x, float z) RandXz(Random rng)
    {
        float max = size - margin;
        float x = MathHelper.Lerp(margin, max, (float)rng.NextDouble());
        float z = MathHelper.Lerp(margin, max, (float)rng.NextDouble());
        return (x, z);
    }
    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    private static Quaternion Yaw(float radians) => Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians);
}