#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class RenderEntityBus
{
    private const int DefaultCapacity = 128;
    private const int MaxCapacity = 10_000;

    private int _idx = 0;
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];

    private World? _world;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;

    public ModelId CubeId { get; set; }
    public MaterialTagKey EmptyMaterialKey { get; set; }

    internal RenderEntityBus(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    private int ActiveSkyCount => _world?.Sky.IsActive ?? false ? 1 : 0;
    private int ActiveTerrainCount => _world?.Terrain.IsActive ?? false ? 1 : 0;
    public int DrawCount => (_world?.EntityCount ?? 0) + ActiveSkyCount + ActiveTerrainCount;

    internal bool IsAttached => _world is not null;

    internal void AttachWorld(World world) => _world = world;

    public void Reset()
    {
        _idx = 0;
    }

/*
    private bool hasRunEntities = false;
    private void DrawBounds()
    {
        if (hasRunEntities)
        {
            _idx *= 2;
            return;
        }

        var idx = _idx;
        Span<Vector3> corners = stackalloc Vector3[8];
        foreach (ref readonly var entity in _entities.AsSpan(0, idx))
        {
            ref var boxEntity = ref _entities[_idx++];
            ref readonly var bounds = ref _world.Entities.BoundingBoxes.GetById(entity.Entity);
            ref readonly var transform = ref entity.Transform;

            MatrixMath.CreateModelMatrix(in transform.Translation, in transform.Scale,
                in transform.Rotation, out var world);

            bounds.Box.FillCorners(corners);

            for (var i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector3.Transform(corners[i], world);
            }

            BoundingAxisBox.FromPoints(corners, out var axisBounds);

            boxEntity.Entity = entity.Entity;
            boxEntity.Model = CubeId;
            boxEntity.MaterialKey = EmptyMaterialKey;
            boxEntity.Transform = new Transform(axisBounds.Center, axisBounds.Extent, in transform.Rotation);
            boxEntity.Meta = new DrawCommandMeta(DrawCommandId.Model, DrawCommandQueue.OverlayTransparent,
                DrawCommandResolver.BoundingVolume, PassMask.Effect);
        }

        hasRunEntities = true;
    }
*/
    public void CollectEntities(in Matrix4x4 viewMat, in ProjectionInfoData projInfo)
    {
        if (_world is null) return;

        var worldEntities = _world.Entities;
        var selected = WorldActionSlot.SelectedEntityId;

        float near = projInfo.Near, far = projInfo.Far;

        EnsureCapacity(DrawCount);

        var idxCollect = 0;
        foreach (var query in worldEntities.Query<ModelComponent, Transform>())
        {
            //Debug.Assert(model.Model != default && model.MaterialKey != default);
            ref var model = ref query.Component1;
            ref var transform = ref query.Component2;

            ref var entity = ref _entities[idxCollect++];

            var depthKey = DepthKeyUtility.MakeDepthKey(in viewMat, in transform.Translation, near, far);

            var meta = new DrawCommandMeta(DrawCommandId.Model, DrawCommandQueue.Opaque, DrawCommandResolver.None,
                PassMask.Default, depthKey);

            if (query.Entity == selected)
            {
                meta = meta.WithResolvePass(DrawCommandResolver.Highlight, PassMask.Effect | PassMask.DepthPre);
            }

            entity.Entity = query.Entity;
            entity.Model = model.Model;
            entity.MaterialKey = model.MaterialKey;
            entity.Transform = transform;
            entity.Meta = meta;
        }

        _idx = idxCollect;
    }

    private readonly Matrix4x4[] _animationGlobals = new Matrix4x4[64];
    private readonly Matrix4x4[] _animationFinal = new Matrix4x4[64];

    
    float t = 0;

    public void ProcessAnimations(float deltaTime, DrawCommandBuffer buffer)
    {
        //var submitView = _meshTable.GetBoneUploadPayload();
        var worldEntities = _world!.Entities;
        var globals = _animationGlobals;
        var finals = _animationFinal;

        t += deltaTime;
        // var idxAnimation = 0;
        foreach (var query in worldEntities.Query<AnimationComponent>())
        {
            ref var component = ref query.Component;

            var modelAnimation = _meshTable.GetAnimationFor(component.Model);
            var animation = modelAnimation.AnimationDataSpan[0];
            var parentIndices = modelAnimation.ParentIndices;

            ref readonly var invMatrix = ref modelAnimation.InverseRootTransform;
            var boneByIndex = animation.BoneTracksMap;
            var boneCount = boneByIndex.Count;
            var boneTransforms = modelAnimation.GetBoneTransformSpan();

            if ((uint)boneCount > globals.Length || (uint)boneCount > finals.Length ||
                (uint)boneCount > parentIndices.Length)
                throw new IndexOutOfRangeException();

            globals.AsSpan().Clear();

            Transform poseTransform = default;
            Matrix4x4 local = default;
            for (int i = 0; i < boneCount; i++)
            {
                if (!boneByIndex.TryGetValue(i, out var track))
                {
                    poseTransform = Transform.Baseline;
                }
                else
                {
                    poseTransform.Translation = LerpVector(track.Translations, track.TranslationTimes, t, default);
                    poseTransform.Scale = LerpVector(track.Scales, track.ScaleTimes, t, Vector3.One);
                    poseTransform.Rotation = LerpQuaternion(track.Rotations, track.RotationTimes, t);
                }
/*
                Console.WriteLine("Translation");
                Console.WriteLine(poseTransform.Translation.ToString());
                Console.WriteLine("Scale");
                Console.WriteLine(poseTransform.Scale.ToString());
                Console.WriteLine("Rotation");
                Console.WriteLine(poseTransform.Rotation.ToString());
*/
                WriteTransformMatrix(in poseTransform, ref local);

                int p = parentIndices[i];
                if (p >= 0)
                    MatrixMath.MultiplyAffine(in local, in globals[p], out globals[i]);
                else
                    globals[i] = local;

                finals[i] = boneTransforms[i] * globals[i] * invMatrix;
            }

            buffer.SubmitSingleAnimation(finals);
        }
        //MatrixMath.MultiplyAffine(in invMatrix, in globals[i], out finals[i]);
        //MatrixMath.MultiplyAffine(in finals[i], in boneTransforms[i], out finals[i]);

        static Vector3 LerpVector(Vector3[] values, float[] times, float time, Vector3 fallback)
        {
            if (times.Length == 0) return fallback;
            if (times.Length == 1 || time <= times[0]) return values[0];
            if (time >= times[^1]) return values[^1];

            var i = 0;
            while (i < times.Length - 1 && time >= times[i + 1]) i++;

            var f = (time - times[i]) / (times[i + 1] - times[i]);
            return Vector3.Lerp(values[i], values[i + 1], f);
        }

        static Quaternion LerpQuaternion(Quaternion[] values, float[] times, float time)
        {
            if (times.Length == 0) return Quaternion.Identity;
            if (times.Length == 1 || time <= times[0]) return values[0];
            if (time >= times[^1]) return values[^1];

            var i = 0;
            while (i < times.Length - 1 && time >= times[i + 1]) i++;

            var f = (time - times[i]) / (times[i + 1] - times[i]);
            return Quaternion.Slerp(values[i], values[i + 1], f);
        }
    }

    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        buffer.EnsureBufferCapacity(_world.EntityCount + 64);

        FlushWorldEntities(buffer);

        var prevModel = new ModelId(-1);
        var prevMatKey = new MaterialTagKey(-1);

        MaterialTag materialTag = default;
        ModelPartView modelView = default;
        ReadOnlySpan<MaterialId> matSpan = default;

        // stack space for nested loop
        DrawObjectUniform drawData = default;

        var entitySpan = _entities.AsSpan(0, _idx);

        foreach (ref var entity in entitySpan)
        {
            if (entity.Model != prevModel)
            {
                modelView = _meshTable.GetPartsRefView(entity.Model);
                prevModel = entity.Model;
            }

            if (entity.MaterialKey != prevMatKey)
            {
                _materialTable.ResolveSubmitMaterial(entity.MaterialKey, out materialTag);
                matSpan = materialTag.AsReadOnlySpan();
                prevMatKey = entity.MaterialKey;
            }

            Matrix4x4 world = default;
            ref var worldRef = ref Unsafe.AsRef(ref world);
            WriteTransformMatrix(in entity.Transform, ref worldRef);

            var parts = modelView.Parts;
            var locals = modelView.Locals;

            ref var mat0 = ref MemoryMarshal.GetReference(matSpan);

            var baseMeta = entity.Meta;
            int len = int.Min(locals.Length, parts.Length);
            for (var i = 0; i < len; i++)
            {
                ref readonly var part = ref parts[i];

                ref var draw = ref Unsafe.AsRef(ref drawData);
                ApplyTransform(ref draw, in locals[i], in world);

                var meta = BuildMeta(ref Unsafe.AsRef(ref materialTag), part.MaterialSlot, baseMeta);
                ref var mat = ref Unsafe.Add(ref mat0, part.MaterialSlot);
                var cmd = new DrawCommand(part.Mesh, mat, part.DrawCount);
                buffer.SubmitDraw(cmd, meta, ref draw);
            }

            prevMatKey = entity.MaterialKey;
            prevModel = entity.Model;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ApplyTransform(ref DrawObjectUniform data, in Matrix4x4 local, in Matrix4x4 world)
    {
        MatrixMath.MultiplyAffine(in local, in world, out data.Model);
        MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DrawCommandMeta BuildMeta(ref MaterialTag tag, int slot, DrawCommandMeta meta)
    {
        if (!tag.IsTransparent(slot)) return meta;
        var depthKey = (ushort)(ushort.MaxValue - meta.DepthKey);
        return meta.WithTransparency(DrawCommandQueue.Transparent, depthKey);
    }

    private void FlushWorldEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        if (ActiveSkyCount > 0)
        {
            var sky = _world.Sky;
            var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
            var cmd = new DrawCommand(sky.Mesh, sky.Material);

            CreateTransformMatrices(in sky.Transform, out var model, out var normal);
            buffer.SubmitDraw(cmd, meta, in model, in normal);
        }

        if (ActiveTerrainCount > 0)
        {
            var terrain = _world.Terrain;
            var view = _meshTable.GetPartsRefView(terrain.Model);

            var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
            var cmd = new DrawCommand(view.Parts[0].Mesh, terrain.Material);

            CreateTransformMatrices(in Transform.Baseline, out var model, out var normal);
            buffer.SubmitDraw(cmd, meta, in model, in normal);
        }

        //Blend = On, SampleAlphaCoverage = Off, DepthWrite = Off
        // Or
        //Blend = Off, SampleAlphaCoverage = On, DepthWrite = Off

        if (_world.Particles.IsActive)
        {
            var particles = _world.Particles;

            var cmd = new DrawCommand(particles.Mesh, particles.Material, instanceCount: particles.ParticleCount);
            var meta = new DrawCommandMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, passMask: PassMask.Main);
            buffer.SubmitNonTransformDraw(cmd, meta);
        }
    }

    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        out Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(
            transform.Translation,
            transform.Scale,
            transform.Rotation,
            out model
        );

        MatrixMath.CreateNormalMatrix(in model, out normal);
    }


    private void EnsureCapacity(int amount)
    {
        if (_entities.Length >= amount) return;
        var newCap = ArrayUtility.CapacityGrowthToFit(amount, Math.Max(amount, 4));

        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void WriteTransformMatrix(ref readonly Transform transform, ref Matrix4x4 mat)
    {
        float x = transform.Rotation.X, y = transform.Rotation.Y, z = transform.Rotation.Z, w = transform.Rotation.W;
        float xx = x + x, yy = y + y, zz = z + z;
        float xy = x * yy, xz = x * zz, yz = y * zz;
        float wx = w * xx, wy = w * yy, wz = w * zz;
        float x2 = x * xx, y2 = y * yy, z2 = z * zz;

        float r11 = 1f - (y2 + z2), r22 = 1f - (x2 + z2), r33 = 1f - (x2 + y2);
        float r12 = xy + wz, r13 = xz - wy, r21 = xy - wz;
        float r23 = yz + wx, r31 = xz + wy, r32 = yz - wx;

        mat.M11 = r11 * transform.Scale.X;
        mat.M12 = r12 * transform.Scale.Y;
        mat.M13 = r13 * transform.Scale.Z;
        mat.M14 = 0f;

        mat.M21 = r21 * transform.Scale.X;
        mat.M22 = r22 * transform.Scale.Y;
        mat.M23 = r23 * transform.Scale.Z;
        mat.M24 = 0f;

        mat.M31 = r31 * transform.Scale.X;
        mat.M32 = r32 * transform.Scale.Y;
        mat.M33 = r33 * transform.Scale.Z;
        mat.M34 = 0f;

        mat.M41 = transform.Translation.X;
        mat.M42 = transform.Translation.Y;
        mat.M43 = transform.Translation.Z;
        mat.M44 = 1f;
    }
}