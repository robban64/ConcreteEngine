using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Engine.Input;

public static partial class EngineInput
{
    public static class Keyboard
    {
        private static int _keyStateCount;
        private static readonly (int Key, InputButtonState State)[] KeyState = new(int, InputButtonState)[32];

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
            var index = FindIndex((int)key);
            if (index != -1)
            {
                state = KeyState[index].State;
                return true;
            }

            state = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearKeys() => KeyChars.Clear();

        internal static void UpdateKeys()
        {
            var removeCount = 0;
            for (var i = 0; i < KeysToRemove.Count; i++)
            {
                var index = FindIndex(KeysToRemove[i]);
                if (index != -1)
                {
                    KeyState[index] = default;
                    ++removeCount;
                }
            }

            if (removeCount > 0)
            {
                var count = _keyStateCount;
                while (count > 0 && KeyState[count - 1] == default) count--;
                _keyStateCount = count;
            }

            ActiveKeys.Clear();
            KeysToRemove.Clear();

            var length = _keyStateCount;
            for (var i = 0; i < length; i++)
            {
                ref var keyState = ref KeyState[i];
                if (keyState == default) continue;
                keyState.State.Update();
                if (keyState.State is { Up: true, Pressed: false })
                    KeysToRemove.Add(keyState.Key);

                ActiveKeys.Add(keyState.Key);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindIndex(int key)
        {
            var keys = KeyState;
            for (var i = 0; i < keys.Length; i++)
            {
                if (key == keys[i].Key) return i;
            }

            return -1;
        }


        // Keyboard callbacks
        private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
        {
            var index = FindIndex((int)key);
            if (index == -1)
            {
                if (_keyStateCount >= KeyState.Length) Throwers.InvalidOperation("Too many keys");
                index = _keyStateCount++;
            }

            KeyState[index] = ((int)key, new InputButtonState { Down = true, Up = false });
        }
        
        private static void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
        {
            var index = FindIndex((int)key);
            if (index != -1) KeyState[index].State.Up = true;
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