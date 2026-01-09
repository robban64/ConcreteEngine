using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Data;

namespace ConcreteEngine.Editor.Bridge;

public sealed class SceneObjectView(SceneObjectId id, Guid gId, string name, bool enabled)
{
    public readonly SceneObjectId Id = id;

    public readonly Guid GId = gId;
    public readonly string GIdString = gId.ToString();

    public readonly string Name = name;
    public readonly bool Enabled = enabled;

    public required TransformStable EditTransform;

    public required List<ISceneObjectProperty> Properties;

    public required int RenderEcsCount;
    public required int GameEcsCount;

    public bool TryGetProperty<T>(out T value) where T : class
    {
        foreach (var p in Properties)
        {
            if (p is SceneObjectProperty<T> typed)
            {
                value = typed.Value;
                return true;
            }
        }

        value = null!;
        return false;
    }

    public T GetProperty<T>() where T : class
    {
        foreach (var p in Properties)
        {
            if (p is SceneObjectProperty<T> typed)
                return typed.Value;
        }

        throw new KeyNotFoundException($"Property '{name}' of type {typeof(T).Name} not found.");
    }

    public void SetProperty<T>(T value) where T : class
    {
        foreach (var p in Properties)
        {
            if (p is SceneObjectProperty<T> typed)
            {
                if (!typed.IsReadOnly) typed.Value = value;
                return;
            }
        }
    }
}

public interface ISceneObjectProperty
{
    string Name { get; }
    Type PropertyType { get; }
    bool IsMixed { get; }
    bool IsReadOnly { get; }

    object GetValue();
    void SetValue(object value);
}

public class SceneObjectProperty<T>(string name, T value) : ISceneObjectProperty where T : class
{
    public SceneObjectProperty(T value) : this(string.Empty, value) { }
    public Type PropertyType => typeof(T);

    public T Value = value;

    public string Name { get; } = name;

    public bool IsMixed { get; private set; }
    public bool IsReadOnly { get; init; } = false;

    public object GetValue() => Value;
    public void SetValue(object value) => Value = (T)value;
}


public sealed class RenderValue(ModelId model, MaterialId[] materials)
{
    public readonly ModelId Model = model;
    public readonly MaterialId[] Materials = materials;
}

public sealed class ParticleValue(int emitterHandle, MaterialId material, int particleCount)
{
    public readonly int EmitterHandle = emitterHandle;
    public readonly int ParticleCount = particleCount;

    public readonly MaterialId Material = material;

    public ParticleDefinition Definition;
    public ParticleEmitterState EmitterState;
}

public sealed class AnimationValue(AnimationId animationId, int clipCount)
{
    public readonly AnimationId Animation = animationId;
    public readonly int ClipCount = clipCount;

    public int Clip;
    public float Time;
    public float Speed;
    public float Duration;
}