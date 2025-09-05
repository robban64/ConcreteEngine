using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;


internal sealed class CommandDrawerContext
{
    public IGraphicsDevice Graphics { get; init; }
    public MaterialBinder MaterialBinder { get; init; }
    public Render2D Render2D { get; set; }
    public Render3D Render3D { get; set; }

}

internal sealed class CommandDrawerRegistry
{
    private readonly Dictionary<Type, ICommandDrawer> _registry = new(8);
    private readonly List<ICommandDrawer> _drawers = new(8);
    
    private readonly IGraphicsDevice _graphics;
    private readonly MaterialBinder _materialBinder;
    private CommandDrawerContext _context;
    
    public IReadOnlyList<ICommandDrawer> Drawers => _drawers;

    internal CommandDrawerRegistry(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        _graphics = graphics;
        _materialBinder = new MaterialBinder(graphics, materialStore);

    }

    public void Initialize(Render2D render2D, Render3D render3D)
    {
        _context = new CommandDrawerContext
        {
            Graphics = _graphics,
            MaterialBinder = _materialBinder,
            Render2D = render2D,
            Render3D = render3D
        };


        foreach (var renderer in _drawers)
        {
            renderer.AttachContext(_context);
        }
    }

    public void Register<TRenderer>() where TRenderer : ICommandDrawer, new()
    {
        if (!_registry.TryAdd(typeof(TRenderer), new TRenderer()))
            throw new InvalidOperationException($"Renderer already {typeof(TRenderer).Name} registered");
        
        _drawers.Add(_registry[typeof(TRenderer)]);
    }
}