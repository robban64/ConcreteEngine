using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Engine.Input;

public static partial class EngineInput
{
    public static class Mouse
    {
        private const int ButtonCapacity = 16;
        private const float SmoothFactor = 0.2f;
        private const float Epsilon = 0.001f;

        private static readonly InputButtonState[] MouseButtonState = new InputButtonState[ButtonCapacity];

        private static Vector2 _screenPos;
        private static Vector2 _viewportPos;
        private static Vector2 _delta;
        private static Vector2 _scroll;

        private static Vector2 _lastScreenPos;

        private static Vector2 _accumScroll;
        private static Vector2 _lastMouseScroll;

        private static int _activeMouseButtonCount;

        public static Vector2 ScreenPos => _screenPos;
        public static Vector2 ViewportPos => _viewportPos;
        public static Vector2 Delta => _delta;
        public static Vector2 Scroll => _scroll;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InputButtonState GetButton(MouseButton button) => MouseButtonState[(int)button];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<InputButtonState> GetButtonSpan() => MouseButtonState.AsSpan();

        internal static void UpdateMouse()
        {
            var step = (_accumScroll - _lastMouseScroll) * SmoothFactor;
            _scroll = step;
            if (float.Abs(step.X) > Epsilon || float.Abs(step.Y) > Epsilon)
            {
                _lastMouseScroll += step;
            }
            else
            {
                _scroll = Vector2.Zero;
                _lastMouseScroll = _accumScroll;
            }

            _delta = _screenPos - _lastScreenPos;
            _lastScreenPos = _screenPos;
            _viewportPos = _screenPos - EngineWindow.Viewport.Position;

            foreach (ref var state in MouseButtonState.AsSpan(0, _activeMouseButtonCount))
            {
                if (state is { Down: false, WasDown: false, Up: false }) continue;
                state.Update();

                if (state is { Up: true, WasDown: false })
                {
                    state = default;
                    _activeMouseButtonCount--;
                }
            }

            if (_activeMouseButtonCount < 0) _activeMouseButtonCount = 0;
        }

        // Mouse callbacks
        private static void OnMouseMove(IMouse _, Vector2 position) => _screenPos = position;

        private static void OnMouseScroll(IMouse _, ScrollWheel scroll) =>
            _accumScroll += new Vector2(scroll.X, scroll.Y);

        private static void OnMouseDown(IMouse _, MouseButton button)
        {
            int index = (int)button;
            if ((uint)index >= (uint)MouseButtonState.Length) return;

            ref var buttonState = ref MouseButtonState[index];
            if (!buttonState.Down)
                _activeMouseButtonCount++;

            buttonState.Down = true;
            buttonState.Up = false;
        }

        private static void OnMouseUp(IMouse _, MouseButton button)
        {
            int index = (int)button;
            if ((uint)index >= (uint)MouseButtonState.Length) return;
            MouseButtonState[index].Up = true;
        }

        internal static void Attach(IMouse mouse)
        {
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseScroll;
        }

        internal static void Detach(IMouse mouse)
        {
            mouse.MouseDown -= OnMouseDown;
            mouse.MouseUp -= OnMouseUp;
            mouse.MouseMove -= OnMouseMove;
            mouse.Scroll -= OnMouseScroll;
        }
    }
}