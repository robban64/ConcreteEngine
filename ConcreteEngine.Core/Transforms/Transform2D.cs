#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Extensions;

#endregion

namespace ConcreteEngine.Core.Transforms;


public sealed class WorldTransform
{
    private Matrix4x4 _transformMat =  Matrix4x4.Identity;
    private Vector2 _position = Vector2.Zero;
    private Vector2 _scale = Vector2.Zero;
    private float _rotation;

    public Vector2 Position => _position;
    public Vector2 Scale => _scale;
    public float Rotation => _rotation;
    public Matrix4x4 TransformMatrix => _transformMat;

    internal void UpdateWorldTransform(Transform2D localTransform, WorldTransform? parentWorldTransform)
    {
        if (parentWorldTransform != null)
        {
            _transformMat = parentWorldTransform.TransformMatrix * localTransform.TransformMatrix;
            _position = parentWorldTransform.Position *  localTransform.Position;
            _scale = parentWorldTransform.Scale * localTransform.Scale;
            _rotation = parentWorldTransform.Rotation * localTransform.Rotation;
        }
        else
        {
            _transformMat = localTransform.TransformMatrix;
            _position = localTransform.Position;
            _scale = localTransform.Scale;
            _rotation = localTransform.Rotation;
        }
    }

}
public sealed class Transform2D
{
    private bool _dirty = true;
    private bool _world = false;

    private Vector2 _position = Vector2.Zero;
    private Vector2 _scale = Vector2.One;
    private float _rotation = 0;
    
    private Matrix4x4 _transformMat = Matrix4x4.Identity;
    
    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            _dirty = true;
        }
    }
    
    public Vector2 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            _dirty = true;
        }
    }
    
    public float Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _dirty = true;
        }
    }

    public Matrix4x4 TransformMatrix
    {
        get
        {
            if (_dirty)
            {
                _transformMat = CreateTransformMatrix(_position, _scale,  _rotation);
                _dirty = false;
            }
            return _transformMat;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTransformMatrix(Vector2 position, Vector2 scale,
        float rotation)
    {
        var translationMat = Matrix4x4.CreateTranslation(position.ToVec3());
        var rotationMat = Matrix4x4.CreateRotationZ(rotation);
        var scaleMat = Matrix4x4.CreateScale(scale.ToVec3(1));

        return scaleMat * rotationMat * translationMat;
    }


    public static Transform2D Identity => new();
}