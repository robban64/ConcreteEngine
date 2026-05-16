using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS.Integration;

namespace ConcreteEngine.Core.Engine.ECS;

public enum EcsStoreType : byte
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

    protected sealed class EcsStoreMeta
    {
        public readonly int StoreId = ++_currentStoreId;
        public bool IsDirty;
        public readonly Stack<int> Free = [];
        public readonly List<Action<EcsStore>> OnResizeCallbacks = [];
        public readonly List<IEntityListener> Listeners = [];
    }

    protected readonly EcsStoreMeta StoreMeta = new();

    public int Count { get; protected set; }

    public bool IsDirty => StoreMeta.IsDirty;
    public int ActiveCount => Count - StoreMeta.Free.Count;

    public abstract int Capacity { get; }
    public abstract EcsStoreType StoreType { get; }

    public abstract Span<int> GetRawEntities();
    
    internal abstract void Initialize();
    protected abstract void Resize(int newSize);


    public void AddResizeCallback(Action<EcsStore> callback) => StoreMeta.OnResizeCallbacks.Add(callback);
    public void RemoveResizeCallback(Action<EcsStore> callback) => StoreMeta.OnResizeCallbacks.Remove(callback);

    public void BindListener(IEntityListener listener) => StoreMeta.Listeners.Add(listener);
    public void UnbindListener(IEntityListener listener) => StoreMeta.Listeners.Remove(listener);


    protected int AllocateNext()
    {
        var index = StackUtils.NextSlot(StoreMeta.Free, Count);
        if(index >= 0) return index;

        if (Count >= Capacity) EnsureCapacity(1);
        return Count++;
    }

    protected void FreeEntity(int index)
    {
        StoreMeta.IsDirty = true;
        
        var count  = StackUtils.FreeSlot(StoreMeta.Free, index, Count, GetRawEntities(), 0);
        Count = count;

        if (index == Count - 1) Count--;
        else StoreMeta.Free.Push(index);

        if (ActiveCount == 0)
        {
            StoreMeta.Free.Clear();
            Count = 0;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int amount)
    {
        var len = Count + amount;
        if (Capacity >= len) return;

        var newSize = Arrays.CapacityGrowthSafe(Capacity, len);
        newSize = IntMath.AlignUp(newSize, 32);

        Resize(newSize);
        Console.WriteLine($"{GetType().Name}: resized {newSize}");

        foreach (var callback in StoreMeta.OnResizeCallbacks)
            callback(this);

        //Logger.LogString(LogScope.World, $"GameEntities: resized {newSize}", LogLevel.Warn);
    }
}