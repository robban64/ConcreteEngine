using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.Scene.Template;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneManager
{
    private readonly World _world;
    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;
    private readonly SceneStore _store;

    public SceneStore Store => _store;
    public int SceneObjectCount => _store.Count;

    internal SceneManager(AssetSystem assetSystem, World world)
    {
        _world = world;
        _assetStore = assetSystem.Store;
        _materialStore = assetSystem.MaterialStore;
        _store = new SceneStore();
    }


    public SceneObjectId CreateSceneObject(string name) => _store.Create(name);

    public EntityTuple SpawnEntity(SceneObjectId id, EntityTemplate template)
    {
        if (template is null || template.GameEntity is null && template.RenderEntity is null)
            throw new ArgumentNullException();

        var sceneObject = _store.Get(id);

        RenderEntityId renderEntityId = default;
        GameEntityId gameEntityId = default;

        if (template.RenderEntity is { } renderTemplate)
            renderEntityId = RenderEntityFactory.BuildRenderEntity(sceneObject, _world, renderTemplate);

        if (template.GameEntity is { } gameTemplate)
        {
            gameEntityId = GameEntityFactory.BuildGameEntity(sceneObject, gameTemplate);
            if (gameTemplate.CreateRenderEntity)
            {
                InvalidOpThrower.ThrowIfNot(renderEntityId.IsValid());
                Ecs.Game.Stores<RenderLink>.Store.Add(gameEntityId, new RenderLink { RenderEntityId = renderEntityId });
            }
        }

        return new EntityTuple(gameEntityId, renderEntityId);
    }
}