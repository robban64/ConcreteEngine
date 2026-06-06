using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Engine.Input;

public static  partial class EngineInput
{
    public static class Keyboard
    {
        private static readonly Dictionary<int, InputButtonState> KeyState = new(16);

        private static readonly List<int> ActiveKeys = new(16);
        private static readonly List<int> KeysToRemove = new(16);
        private static readonly List<char> KeyChars = new(32);

        public static bool HasEmptyKeyChars => KeyChars.Count == 0;
        public static bool HasEmptyKeyInput => ActiveKeys.Count == 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasKey(Key key, out InputButtonState state) => KeyState.TryGetValue((int)key, out state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<Key> GetActiveKeys() =>
            MemoryMarshal.Cast<int, Key>(CollectionsMarshal.AsSpan(ActiveKeys));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> GetKeyChars() => CollectionsMarshal.AsSpan(KeyChars);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearKeys() => KeyChars.Clear();

        internal static void UpdateKeys()
        {
            foreach (var key in KeysToRemove)
                KeyState.Remove(key);

            ActiveKeys.Clear();
            KeysToRemove.Clear();

            foreach (var key in KeyState.Keys)
            {
                ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(KeyState, key, out _);
                state.Update();
                if (state is { Up: true, Pressed: false })
                    KeysToRemove.Add(key);

                ActiveKeys.Add(key);
            }

        }
        
        // Keyboard callbacks
        private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(KeyState, (int)key, out _);
            state.Down = true;
            state.Up = false;
        }

        private static void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(KeyState, (int)key, out bool exists);
            if (exists) state.Up = true;
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