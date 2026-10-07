using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static RenderBitSet CreateSet() => new();
    
    public sealed class RenderBitSet
    {
        private BitSet _set;
        
        internal RenderBitSet()
        {
            if (Instance == null!) Throwers.InvalidOperation(nameof(Instance));
            _set = new  BitSet(EntityCapacity);
            Instance._bitSets.Add(this);
        }
        
        public BitSet Set
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _set;
        }
        
        public bool this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _set[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set =>  _set[index] = value;
        }
        
        internal void Resize(int size) => _set = _set.Resized(size);
    }
}