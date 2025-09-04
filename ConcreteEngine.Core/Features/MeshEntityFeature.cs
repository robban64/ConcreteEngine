using ConcreteEngine.Core.Rendering;

namespace ConcreteEngine.Core.Features;

public sealed class MeshEntityFeature : GameFeature, IDrawableFeature<MeshDrawData>
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;
    public override bool IsUpdateable => true;

    private readonly MeshDrawData _drawData = new ();

    private MeshDrawEntity[] _entities = new  MeshDrawEntity[64];
    private int _idx = 0;

    public override void Initialize()
    {
        
    }

    public override void UpdateTick(int tick)
    {
        _idx = 0;
        
        var world = Context.World;
        
        if (_entities.Length < world.Meshes.Count)
        {
            var newSize = int.Max(_entities.Length * 2, world.Meshes.Count);
            Array.Resize(ref _entities, newSize);

        }
        
        foreach (var view in world.Meshes.View2(world.Transforms))
        {
            ref var mesh = ref view.Value1;
            ref var transform = ref view.Value2;
            _entities[_idx++] = new MeshDrawEntity(mesh.MeshId, mesh.MaterialId, in transform);
        }
    }

    public MeshDrawData GetDrawables()
    {
        _drawData.Count = _idx;
        _drawData.Entities = _entities;
        return _drawData;
    }



}