#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;


public sealed class SpriteBatcher : RenderBatcher<SpriteBatchBuildResult>
{
    private const int MaxSpriteBatchSize = 1024;
    private const int MaxSpriteBatchInstanceCount = 4;
    
    private readonly IGraphicsRuntime _graphics;
    private readonly IGraphicsContext _gfx;

    private int _commandSize = 0;
    private readonly SpriteBatchDrawItem[] _commandBuffer;

    private SpriteBatchMesh? _boundSpriteBatch;
    private readonly SortedList<int, SpriteBatchMesh> _spriteBatches;

    internal SpriteBatcher(IGraphicsRuntime graphics) : base(graphics)
    {
        _graphics = graphics;
        _gfx = graphics.Context;

        _commandBuffer = new SpriteBatchDrawItem[MaxSpriteBatchSize];
        _spriteBatches = new(MaxSpriteBatchInstanceCount);
    }

    internal void Prepare()
    {
        _commandSize = 0;
        _boundSpriteBatch = null;
    }

    public void CreateSpriteBatch(int id, int capacity)
    {
        if (_spriteBatches.Count >= MaxSpriteBatchInstanceCount - 1)
        {
            throw GraphicsException.CapabilityExceeded<SpriteBatcher>(
                "SpriteBatch Count",
                _spriteBatches.Count,
                MaxSpriteBatchInstanceCount
            );
        }

        if (_spriteBatches.ContainsKey(id))
            throw GraphicsException.ResourceAlreadyExists<SpriteBatchMesh>(id);

        _spriteBatches.Add(id, new SpriteBatchMesh(_graphics, capacity));
    }

    public void RemoveSpriteBatch(int id)
    {
        if (!_spriteBatches.TryGetValue(id, out var spriteBatch))
        {
            throw GraphicsException.ResourceNotFound<SpriteBatchMesh>(id);
        }

        spriteBatch.Dispose();
        _spriteBatches.Remove(id);
    }

    public void SubmitSprite(in SpriteBatchDrawItem cmd)
    {
        _commandBuffer[_commandSize] = cmd;
        _commandSize++;
    }

    public void BeginBatch(int id)
    {
        if (!_spriteBatches.TryGetValue(id, out var value))
            GraphicsException.ThrowResourceNotFound<SpriteBatchMesh>(id);

        _commandSize = 0;
        _boundSpriteBatch = value;
    }

    public override SpriteBatchBuildResult BuildBatch()
    {
        if (_boundSpriteBatch == null) GraphicsException.ThrowInvalidState("No sprite batch is bound.");
        if (_commandSize <= 0) GraphicsException.ThrowInvalidState("No commands are available.");

        var commandSpan = _commandBuffer.AsSpan().Slice(0, _commandSize);

        var result = _boundSpriteBatch.BuildSpriteBatch(commandSpan);

        _boundSpriteBatch = null;
        _commandSize = 0;

        return result;
    }

    public override void Dispose()
    {
        _boundSpriteBatch = null;

        foreach (var spriteBatch in _spriteBatches.Values)
            spriteBatch.Dispose();
    }
}