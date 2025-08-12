#region

using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering.Sprite;

public sealed class SpriteBatchController
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;
    private readonly RenderPipeline _renderPipeline;

    private int _commandSize = 0;
    private readonly SpriteBatchDrawItem[] _commandBuffer;

    private SpriteBatch? _boundSpriteBatch;
    private readonly Dictionary<string, SpriteBatch> _spriteBatches;

    private Matrix4X4<float> _transformMatrix = Matrix4X4<float>.Identity;
    private ushort _textureId = 0;
    private ushort _shaderId = 0;

    private static readonly Matrix4X4<float> DefaultTransform =
        Transform2D.CreateTransformMatrix(Vector2D<float>.Zero, Vector2D<float>.One, 0);

    internal SpriteBatchController(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _ctx = graphics.Ctx;
        _renderPipeline = graphics.RenderPipeline;

        _commandBuffer = new SpriteBatchDrawItem[_graphics.Configuration.MaxSpriteBatchSize];
        _spriteBatches = new Dictionary<string, SpriteBatch>(_graphics.Configuration.MaxSpriteBatchInstanceCount);
    }

    internal void Prepare()
    {
        _commandSize = 0;
        _boundSpriteBatch = null;
        _textureId = 0;
        _shaderId = 0;
    }

    public void CreateSpriteBatch(string name, int capacity)
    {
        if (_spriteBatches.Count >= _graphics.Configuration.MaxSpriteBatchInstanceCount - 1)
        {
            throw GraphicsException.CapabilityExceeded<SpriteBatchController>(
                "SpriteBatch Count",
                _spriteBatches.Count,
                _graphics.Configuration.MaxSpriteBatchInstanceCount
            );
        }
        
        if(_spriteBatches.ContainsKey(name))
            throw GraphicsException.ResourceAlreadyExists<SpriteBatch>(name);

        _spriteBatches.Add(name, new SpriteBatch(_graphics, capacity));
    }

    public void RemoveSpriteBatch(string name)
    {
        if (!_spriteBatches.TryGetValue(name, out var spriteBatch))
        {
            throw GraphicsException.ResourceNotFound<SpriteBatch>(name);
        }

        spriteBatch.Dispose();
        _spriteBatches.Remove(name);
    }

    public void SubmitSprite(in SpriteBatchDrawItem cmd)
    {
        //if(_texture == null) throw new InvalidOperationException("No texture is bound to the sprite batch.");
        //if(_shader == null) throw new InvalidOperationException("No shader is bound to the sprite batch.");
        _commandBuffer[_commandSize] = cmd;
        _commandSize++;
    }

    public void BeginBatch(string name, ushort textureId, ushort shaderId)
    {
        BeginBatch(name);
        _textureId = textureId;
        _shaderId = shaderId;
        _transformMatrix = DefaultTransform;
    }

    public void BeginBatch(string name, ushort textureId, ushort shaderId, in Matrix4X4<float> transform)
    {
        BeginBatch(name);
        _textureId = textureId;
        _shaderId = shaderId;
        _transformMatrix = transform;
    }

    private void BeginBatch(string name)
    {
        if (!_spriteBatches.TryGetValue(name, out var value))
            throw GraphicsException.ResourceNotFound<SpriteBatch>(name);

        _commandSize = 0;
        _boundSpriteBatch = value;
    }

    public void FlushBatch()
    {
        if (_boundSpriteBatch == null) throw GraphicsException.InvalidState("No sprite batch is bound.");
        if (_commandSize <= 0) throw GraphicsException.InvalidState("No commands are available.");
        if (_textureId == 0) throw GraphicsException.InvalidState("No texture is bound to the sprite batch.");
        if (_shaderId == 0) throw GraphicsException.InvalidState("No shader is bound to the sprite batch.");

        var commandSpan = _commandBuffer.AsSpan().Slice(0, _commandSize);
        var cmd = _boundSpriteBatch.BuildMesh(commandSpan);
        cmd.ShaderId = _shaderId;
        cmd.Transform = _transformMatrix;
        cmd.TextureId = _textureId;
        _renderPipeline.SubmitDraw(cmd);

        _boundSpriteBatch = null;
        _textureId = 0;
        _shaderId = 0;
        _commandSize = 0;
    }
}