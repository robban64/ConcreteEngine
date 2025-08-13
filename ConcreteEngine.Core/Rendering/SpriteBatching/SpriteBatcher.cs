#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering.SpriteBatching;



public sealed class SpriteBatcher
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;

    private int _commandSize = 0;
    private readonly SpriteDrawData[] _commandBuffer;

    private SpriteBatchMesh? _boundSpriteBatch;
    private readonly SortedList<int, SpriteBatchMesh> _spriteBatches;

    private static readonly Matrix4X4<float> DefaultTransform =
        Transform2D.CreateTransformMatrix(Vector2D<float>.Zero, Vector2D<float>.One, 0);

    private Matrix4X4<float> _transformMatrix = Matrix4X4<float>.Identity;
    private ushort _textureId = 0;
    private ushort _shaderId = 0;

    internal SpriteBatcher(IGraphicsDevice graphics, RenderPipeline renderer)
    {
        _graphics = graphics;
        _ctx = graphics.Ctx;

        _commandBuffer = new SpriteDrawData[_graphics.Configuration.MaxSpriteBatchSize];
        _spriteBatches = new (_graphics.Configuration.MaxSpriteBatchInstanceCount);
    }

    internal void Prepare()
    {
        _commandSize = 0;
        _boundSpriteBatch = null;
        _textureId = 0;
        _shaderId = 0;
    }

    public void CreateSpriteBatch(int id, int capacity)
    {
        if (_spriteBatches.Count >= _graphics.Configuration.MaxSpriteBatchInstanceCount - 1)
        {
            throw GraphicsException.CapabilityExceeded<SpriteBatcher>(
                "SpriteBatch Count",
                _spriteBatches.Count,
                _graphics.Configuration.MaxSpriteBatchInstanceCount
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

    public void SubmitSprite(in SpriteDrawData cmd)
    {
        _commandBuffer[_commandSize] = cmd;
        _commandSize++;
    }
    public void BeginBatch(int id, ushort textureId, ushort shaderId)
    {
        BeginBatch(id);
        _textureId = textureId;
        _shaderId = shaderId;
        _transformMatrix = DefaultTransform;
    }

    public void BeginBatch(int id, ushort textureId, ushort shaderId, in Matrix4X4<float> transform)
    {
        if (!_spriteBatches.TryGetValue(id, out var value))
            GraphicsException.ThrowResourceNotFound<SpriteBatchMesh>(id);

        _commandSize = 0;
        _boundSpriteBatch = value;
    }
    
    private void BeginBatch(int id)
    {
        if (!_spriteBatches.TryGetValue(id, out var value))
            GraphicsException.ThrowResourceNotFound<SpriteBatchMesh>(id);

        _commandSize = 0;
        _boundSpriteBatch = value;
    }

    public SpriteDrawCommand FlushBatch()
    {
        if (_boundSpriteBatch == null) GraphicsException.ThrowInvalidState("No sprite batch is bound.");
        if (_commandSize <= 0) GraphicsException.ThrowInvalidState("No commands are available.");
        if (_textureId == 0) GraphicsException.ThrowInvalidState("No texture is bound to the sprite batch.");
        if (_shaderId == 0) GraphicsException.ThrowInvalidState("No shader is bound to the sprite batch.");

        var commandSpan = _commandBuffer.AsSpan().Slice(0, _commandSize);
        
        var result = _boundSpriteBatch.BuildSpriteBatch(commandSpan);
        
        var cmd = new SpriteDrawCommand(
            meshId: result.MeshId,
            shaderId: _shaderId,
            textureId: _textureId,
            drawCount: result.DrawCount,
            transform: in _transformMatrix
        );

        _boundSpriteBatch = null;
        _commandSize = 0;

        return cmd;
    }
}