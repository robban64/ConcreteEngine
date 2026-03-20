using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS.Abstract;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Core.Engine.ECS;

public enum EcsStoreType
{
    Unknown,
    Render,
    RenderCore,
    Game,
    GameCore
}

public abstract class EcsStore
{
    private static int _currentStoreId;

    public readonly int StoreId = ++_currentStoreId;
    
    public bool IsDirty { get; protected set; }
    public int Count { get; protected set; }

    private readonly Stack<int> _free = [];
    
    private readonly List<Action<EcsStore>> _onResizeCallbacks = []; 

    protected EcsStore()
    {
    }
    
    public int ActiveCount => Count - _free.Count;
    
    public abstract int Capacity { get; }
    public abstract EcsStoreType StoreType { get; }
    
    public void AddResizeCallback(Action<EcsStore> callback) => _onResizeCallbacks.Add(callback);
    public void RemoveResizeCallback(Action<EcsStore> callback) => _onResizeCallbacks.Remove(callback);

    protected int AllocateNext()
    {
        if (_free.TryPop(out var index))
            return index;

        EnsureCapacity(1);
        return Count++;
    }

    protected void FreeEntity(int index, int entity)
    {
        _free.Push(index);
        IsDirty = true;
    }

    internal abstract void Initialize();
    protected abstract void Resize(int newSize);
    
    public void EnsureCapacity(int amount)
    {
        var len = Count + amount;
        if (Capacity >= len) return;
        
        var newSize = Arrays.CapacityGrowthSafe(Capacity, len);
        newSize = IntMath.AlignUp(newSize, 32);
        
        Resize(newSize);
        Console.WriteLine($"{GetType().Name}: resized {newSize}");

        foreach (var callback in _onResizeCallbacks)
            callback(this);
        
        //Logger.LogString(LogScope.World, $"GameEntities: resized {newSize}", LogLevel.Warn);
    }


}