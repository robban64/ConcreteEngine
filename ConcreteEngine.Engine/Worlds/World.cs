#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public interface IWorld
{
    int EntityCount { get; }

    Camera3D Camera { get; }
    WorldEntities Entities { get; }

    WorldRenderParams WorldRenderParams { get; }
    WorldSkybox Sky { get; }
    WorldTerrain Terrain { get; }
    WorldParticles Particles { get; }

    WorldRaycaster Raycast { get; }


    IMeshTable MeshTable { get; }
    IMaterialTable EntityMaterials { get; }
}

public sealed class World : IWorld
{
    public Camera3D Camera { get; }
    public WorldRenderParams WorldRenderParams { get; }
    public WorldRaycaster Raycast { get; }

    private readonly MeshGeneratorRegistry _meshGenerators;

    private readonly WorldEntities _entities;
    private readonly WorldSkybox _sky;
    private readonly WorldTerrain _terrain;
    private readonly WorldParticles _particles;

    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;
    private AnimationTable _animationTable = null!;

    internal World()
    {
        Camera = new Camera3D();
        WorldRenderParams = new WorldRenderParams();

        _meshGenerators = new MeshGeneratorRegistry();

        _entities = new WorldEntities();
        _sky = new WorldSkybox();
        _terrain = new WorldTerrain();
        _particles = new WorldParticles();

        Raycast = new WorldRaycaster(Camera, Entities, _terrain);
    }

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;
    public WorldEntities Entities => _entities;
    public WorldParticles Particles => _particles;


    public IMeshTable MeshTable => _meshTable;
    public IMaterialTable EntityMaterials => _materialTable;

    internal MeshTable GetMeshTableImpl() => _meshTable;
    internal MaterialTable GetMaterialTableImpl() => _materialTable;

    internal AnimationTable GetAnimationTableImpl() => _animationTable;


    public int EntityCount => Entities.EntityCount;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;


    internal void AttachRender(GfxContext gfx, MeshTable meshTable, MaterialTable materialTable,
        AnimationTable animationTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
        _animationTable = animationTable;

        Entities.AttachRender(_meshTable, _materialTable);
        Sky.AttachRenderer(_meshTable);
        Terrain.AttachRenderer(_meshGenerators.Register(new TerrainMeshGenerator(gfx)), _meshTable, _materialTable);
        _particles.AttachRenderer(_meshGenerators.Register(new ParticleMeshGenerator(gfx)), _materialTable);
    }

    internal void StartTick(Size2D viewSize, float fixedDt, float totalTime)
    {
        Camera.Viewport = viewSize;
        ProcessActions();
        Camera.StartTick();
    }

    internal void EndTick(RenderCamera renderCamera)
    {
        Entities.EndTick();
        Camera.EndTick(WorldRenderParams.Snapshot, renderCamera);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(_entities, fixedDt);
    }

    internal void StartRenderFrame(float alpha)
    {
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
    }

    private void ProcessActions()
    {
        var entities = Entities;
    }
}