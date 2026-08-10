using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Time;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Engine.Input;

public static partial class EngineInput
{
    public static class Keyboard
    {
        private static int _keyStateCount;
        private static InputButtonState[] _keyState = new InputButtonState[16];

        private static readonly List<int> ActiveKeys = new(16);
        private static readonly List<int> KeysToRemove = new(16);
        private static readonly List<char> KeyChars = new(32);

        public static bool HasEmptyKeyChars => KeyChars.Count == 0;
        public static bool HasEmptyKeyInput => ActiveKeys.Count == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<Key> GetActiveKeys() =>
            MemoryMarshal.Cast<int, Key>(CollectionsMarshal.AsSpan(ActiveKeys));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> GetKeyChars() => CollectionsMarshal.AsSpan(KeyChars);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(Key key, out InputButtonState state)
        {
            ref var it = ref GetRefOrNull((int)key, out var index);
            if (index != -1)
            {
                state = it;
                return true;
            } 
            state = default;
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearKeys() => KeyChars.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindIndex(int key)
        {
            var length = _keyState.Length;
            for (var i = 0; i < length; i++)
            {
                if (key == _keyState[i].Button) return i;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref InputButtonState GetRefOrNull(int key, out int index)
        {
            index = FindIndex(key);
            if(index >= 0) return ref _keyState[index];
            return ref Unsafe.NullRef<InputButtonState>();
        }


        internal static void UpdateKeys()
        {
            var removeCount = 0;
            for (var i = 0; i < KeysToRemove.Count; i++)
            {
                ref var it = ref GetRefOrNull(KeysToRemove[i], out var index);
                if (index != -1)
                {
                    ++removeCount;
                    it = default;
                }
            }

            if (removeCount > 0)
            {
                var count = _keyStateCount;
                while (count > 0 && _keyState[count - 1] == default) count--;
                _keyStateCount = count;
            }

            ActiveKeys.Clear();
            KeysToRemove.Clear();

            var length = _keyStateCount;
            for (var i = 0; i < length; i++)
            {
                ref var key = ref _keyState[i];
                if (key == default) continue;

                key.Update();
                if (key is { Up: true, Pressed: false })
                    KeysToRemove.Add(key.Button);

                ActiveKeys.Add(key.Button);
            }
        }
        
        // Keyboard callbacks
        private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
        {
            var index = FindIndex((int)key);
            if (index == -1)
            {
                if (_keyStateCount >= _keyState.Length)
                {
                    if(_keyState.Length >= 128) Throwers.InvalidOperation("Too many keys");
                    Array.Resize(ref _keyState, _keyState.Length * 2);
                }

                index = _keyStateCount++;
            }
            
            _keyState[index] = new InputButtonState { Button = (int)key, Down = true, Up = false };
        }

        private static void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
        {
            ref var keyState = ref GetRefOrNull((int)key, out var index);
            if (index != -1) keyState.Up = true;
        }

        private static void OnKeyChar(IKeyboard keyboard, char key) => KeyChars.Add(key);

        internal static void Attach(IKeyboard keyboard)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            keyboard.KeyChar += OnKeyChar;
        }

        internal static void Detach(IKeyboard keyboard)
        {
            keyboard.KeyDown -= OnKeyDown;
            keyboard.KeyUp -= OnKeyUp;
            keyboard.KeyChar -= OnKeyChar;
        }
    }
}