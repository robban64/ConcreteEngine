using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Worlds;

public sealed class World : IGameEngineSystem
{
    private readonly AssetSystem _assets;

    private readonly WorldSkybox _sky;
    private readonly WorldTerrain _terrain;
    private readonly WorldParticles _particles;

    private readonly WorldRenderParams _worldRenderParams;

    private readonly WorldRaycaster _raycast;
    private readonly Camera3D _camera;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;
    private readonly AnimationTable _animationTable;
    private readonly MeshGeneratorRegistry _meshGenerator;


    private readonly DrawEntityAssembler _drawEntities;
    private readonly WorldRenderer _worldRenderer;

    private readonly EntityWorld _ecs;


    internal World(EngineWindow engineWindow, GraphicsRuntime graphics, AssetSystem assets, EntityWorld ecs)
    {
        _assets = assets;
        _ecs = ecs;
        _camera = new Camera3D();
        _meshGenerator = new MeshGeneratorRegistry();

        _meshTable = new MeshTable();
        _materialTable = new MaterialTable();
        _animationTable = new AnimationTable();

        _sky = new WorldSkybox();
        _terrain = new WorldTerrain(_meshTable, _materialTable);
        _particles = new WorldParticles(_meshTable, _materialTable);

        _drawEntities = new DrawEntityAssembler(this);

        _raycast = new WorldRaycaster(Camera, Entities, _terrain, _drawEntities);

        _worldRenderParams = new WorldRenderParams(AssetConfigLoader.GraphicSettings);
        _worldRenderParams.EndTick();

        _worldRenderer = new WorldRenderer(engineWindow, graphics, assets, _worldRenderParams, _drawEntities, _camera);
    }

    internal RenderEntityHub Entities => _ecs.RenderEntity;

    internal WorldRenderer Renderer => _worldRenderer;

    public Camera3D Camera => _camera;
    public WorldRaycaster Raycast => _raycast;

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;
    public WorldParticles Particles => _particles;

    public WorldRenderParams WorldRenderParams => _worldRenderParams;

    internal MeshTable MeshTableImpl => _meshTable;
    internal MaterialTable MaterialTableImpl => _materialTable;
    internal AnimationTable AnimationTableImpl => _animationTable;

    public int EntityCount => Entities.EntityCount;

    internal void Initialize(AssetSystem assets, GfxContext gfx)
    {
        _meshTable.Setup(_assets);
        _animationTable.Setup(_assets);

        Terrain.AttachRenderer(_meshGenerator.Register(new TerrainMeshGenerator(gfx)));
        _particles.AttachRenderer(_meshGenerator.Register(new ParticleMeshGenerator(gfx)));
        _sky.AttachRenderer(_meshTable);


        PrimitiveMeshes.Cube = _assets.Store.GetByName<Model>("Cube").MeshParts[0].ResourceId;
        var mat = assets.MaterialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.State.Pipeline = new MaterialPipelineState
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassStateFunc(BlendMode.Alpha)
        };
        _drawEntities.BoundsMaterial = mat.Id;
    }

    internal void BeforeRender()
    {
        var gameEcs = _ecs.GameEntity;
        var renderEcs = _ecs.RenderEntity;
        var alpha = EngineTime.GameAlpha;

        StaticProfileTimer.RenderTimer.Begin();
        var renderAnimations = renderEcs.GetStore<RenderAnimationComponent>();
        foreach (var query in gameEcs.Query<AnimationComponent, RenderLink>())
        {
            ref readonly var a = ref query.Component1;
            ref readonly var renderEntity = ref query.Component2.RenderEntityId;;
            if(renderEntity == default) continue;

            var animationPtr = renderAnimations.TryGet(renderEntity);
            if(animationPtr.IsNull) continue;

            float t;
            if (a.Time < a.PrevTime)
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time + a.Duration, alpha) % a.Duration;
            else 
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time, alpha);

            animationPtr.Value.Speed = a.Speed;
        }
        StaticProfileTimer.RenderTimer.EndPrint();
    }
    
    
    internal void UpdateTick(float dt, Size2D viewport)
    {
        Camera.StartTick(viewport);
    }

    internal void EndUpdateTick(float dt)
    {
        //Entities.EndTick();
        
        var gameEcs = _ecs.GameEntity;
        foreach (var query in gameEcs.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.PrevTime = c.Time;
            
            c.Time += dt * c.Speed;
            if (c.Time > c.Duration) c.Time = 0;
        }
        
        WorldRenderParams.EndTick();
        Camera.EndTick(WorldRenderParams.Snapshot, _worldRenderer.RenderCamera);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(_ecs.RenderEntity, fixedDt);
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
    }



    public void Shutdown() {}
}