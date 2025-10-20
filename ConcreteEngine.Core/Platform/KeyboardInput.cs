#region

using Silk.NET.Input;

#endregion

namespace ConcreteEngine.Core.Platform;

internal sealed class KeyboardInput : IDisposable
{
    private readonly IKeyboard _keyboard;
    private readonly HashSet<Key> _keysDown = [];
    private readonly HashSet<Key> _keysPressed = [];
    private readonly HashSet<Key> _keysReleased = [];
    
    internal bool Enabled { get; set; }

    public KeyboardInput(IKeyboard keyboard)
    {
        _keyboard = keyboard;
        _keyboard.KeyDown += OnKeyDown;
        _keyboard.KeyUp += OnKeyUp;
    }

    public void Update(bool enable)
    {
        Enabled = enable;
        
        _keysPressed.Clear();
        _keysReleased.Clear();
        if(!Enabled) _keysDown.Clear();
        //_keysDown.Clear();
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if(!Enabled) return;
        if (_keysDown.Add(key))
        {
            _keysPressed.Add(key);
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if(!Enabled) return;
        _keysDown.Remove(key);
        _keysReleased.Add(key);
    }

    public bool IsKeyDown(Key key) => _keysDown.Contains(key);
    public bool IsKeyPressed(Key key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(Key key) => _keysReleased.Contains(key);

    public void Dispose()
    {
        _keyboard.KeyDown -= OnKeyDown;
        _keyboard.KeyUp -= OnKeyUp;
    }
}