#region

using System.Numerics;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Utils;
using Silk.NET.Maths;

#endregion

namespace Demo2D;

public class NpcSpriteModule : GameModule
{
    private readonly SpriteAtlas _spriteAtlas = new();

    private int _directionCountdown = 5;

    private int _dirChunckSize = 32;
    private int _dirChunckOffset = 0;

    private int _animationCountdown = 3;
    private int _currentFrame = 0;

    private Vector2[] _entityDirs = null!;

    private Random _random = new();

    private readonly Vector2 _mapUpperBounds = new(64 * 32, 64 * 32);

    public override void Initialize()
    {
    }

    public override void OnSceneReady()
    {
        var spriteTexture = Context.GetSystem<IAssetSystem>().Get<Texture2D>("SpriteTexture");
        _spriteAtlas.Set(new Vector2D<int>(64, 64), new Vector2D<int>(spriteTexture.Width, spriteTexture.Height));

        var spriteSpan = Context.World.Sprites.AsSpan();
        foreach (ref var node in spriteSpan)
        {
            node.UvScale = _spriteAtlas.InvTexSizePx;
        }

        _entityDirs = new Vector2[spriteSpan.Length];
        for (int i = 0; i < _entityDirs.Length; i++)
        {
            var v = _random.Next(0, 4);
            switch (v)
            {
                case 0: _entityDirs[i] = -Vector2.UnitX; break;
                case 1: _entityDirs[i] = Vector2.UnitX; break;
                case 2: _entityDirs[i] = -Vector2.UnitY; break;
                case 3: _entityDirs[i] = Vector2.UnitY; break;
            }
        }
    }

    public override void UpdateTick(int tick)
    {
        const float speed = 2;

        bool shouldAnimate = false;
        bool shouldRotate = false;

        _animationCountdown--;
        if (_animationCountdown == 0)
        {
            if (++_currentFrame % 9 == 0) _currentFrame = 0;
            _animationCountdown = 3;
            shouldAnimate = true;
        }

        _directionCountdown--;
        if (_directionCountdown == 0)
        {
            _directionCountdown = 5;
            shouldRotate = true;
        }

        var spriteStore = Context.World.Sprites;
        var transformStore = Context.World.Transforms2D;
        var directions = _entityDirs.AsSpan();

        int idx = 0;
        if (shouldAnimate)
        {
            var spritesSpan = spriteStore.AsSpan();
            foreach (ref var sprite in spritesSpan)
            {
                var dir = directions[idx];
                var row = 0;
                if (dir.X < 0) row = 1;
                else if (dir.X > 0) row = 3;
                else if (dir.Y < 0) row = 0;
                else if (dir.Y > 0) row = 2;
                sprite.SourceRectangle = _spriteAtlas.At(_currentFrame, row);
                idx++;
            }

            idx = 0;
        }

        foreach (var it in spriteStore)
        {
            ref var transform = ref transformStore.Get(it.Entity);
            ref var dir = ref _entityDirs[idx];
            transform.Position += dir * speed;

            if (transform.Position.Y > _mapUpperBounds.Y) dir.Y = -1;
            else if (transform.Position.Y < 0) dir.Y = 1;
            if (transform.Position.X > _mapUpperBounds.X) dir.X = -1;
            else if (transform.Position.X < 0) dir.X = 1;
            idx++;
        }

        if (shouldRotate)
        {
            var end = _entityDirs.Length - 1;
            var diff = end - _dirChunckOffset;

            if (diff <= 0)
            {
                _dirChunckOffset = 0;
                diff = end - _dirChunckSize;
            }

            var length = int.Min(_dirChunckSize, diff);
            var dirChunk = directions.Slice(_dirChunckOffset, length);

            for (int i = 0; i < dirChunk.Length; i++)
            {
                var v = _random.Next(0, 4);
                switch (v)
                {
                    case 0: dirChunk[i] = -Vector2.UnitX; break;
                    case 1: dirChunk[i] = Vector2.UnitX; break;
                    case 2: dirChunk[i] = -Vector2.UnitY; break;
                    case 3: dirChunk[i] = Vector2.UnitY; break;
                }
            }

            _dirChunckOffset += _dirChunckSize;
            if (_dirChunckOffset >= _entityDirs.Length - 1)
            {
                _dirChunckOffset = 0;
            }
        }
    }
}