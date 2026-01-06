using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

internal sealed class ImGuiController(IWindow window, EditorEngineController engine)
{
    public bool Initialized { get; private set; }

    private ImGuiContextPtr _imGuiContext;
    private ImGuiIOPtr _io;

    private ImDrawDataPtr _cachedDrawData;
    private bool _hasCachedDrawData;

    private float _scale;

    public static bool IsBlockInput { get; private set; }
    public static bool IsMouseOverEditor { get; private set; }

    public unsafe void Setup(string fontFile, float scale)
    {
        if (Initialized) throw new InvalidOperationException("ImGuiRenderer already initialized");

        _scale = scale;

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        var io = _io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImGuiImplGLFW.SetCurrentContext(_imGuiContext);

        var windowPtr = (GLFWwindow*)window.Handle;
        ImGuiImplOpenGL3.SetCurrentContext(_imGuiContext);
        ImGuiImplOpenGL3.Init("#version 330");
        ImGuiImplGLFW.InitForOpenGL(windowPtr, false);

        ImGui.StyleColorsDark();

        io.DisplaySize = (Vector2)window.Size;
        io.DisplayFramebufferScale = Vector2.One;

        io.Fonts.Clear();
        io.Fonts.AddFontFromFileTTF(fontFile, 14.0f * _scale);
        GuiTheme.SetTheme(_scale);

        Initialized = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateInputChar()
    {
        _io.MousePos = engine.Mouse.Position;
        _io.MouseDown[0] = engine.IsMouseDown(MouseButton.Left);
        _io.MouseDown[1] = engine.IsMouseDown(MouseButton.Right);
        _io.MouseDown[2] = engine.IsMouseDown(MouseButton.Middle);

        _io.MouseWheel = engine.Mouse.Scroll.Y;
        _io.MouseWheelH = engine.Mouse.Scroll.X;
        
        if(engine is { HasEmptyKeyChars: true, HasEmptyKeyInput: true }) return;

        foreach (var key in engine.GetActiveKeys())
            _io.AddKeyEvent((ImGuiKey)ImGuiKeyMapper.KeyMap[(int)key], !engine.IsKeyUp(key));

        foreach (var key in engine.GetKeyChars())
            _io.AddInputCharacter(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFrameData(float deltaTime, Size2D windowSize)
    {
        _io.DisplaySize = windowSize.ToVector2();
        _io.DisplayFramebufferScale = Vector2.One;
        _io.DeltaTime = deltaTime;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NewFrame()
    {
        ImGuiImplOpenGL3.NewFrame();
        ImGui.NewFrame();
        //ImGuizmo.BeginFrame();
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndFrame()
    {
        /*
        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
            window.MakeCurrent();
        }
        */

        ImGui.Render();
        _cachedDrawData = ImGui.GetDrawData();
        _hasCachedDrawData = true;
        
        IsBlockInput = _io.WantTextInput ||  ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused();
        IsMouseOverEditor = _io.WantCaptureMouse;
        engine.ToggleBlockInput(IsBlockInput ||  IsMouseOverEditor); 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderDrawData()
    {
        if (!_hasCachedDrawData || _cachedDrawData.DisplaySize is not { X: > 0, Y: > 0 }) return;
        ImGuiImplOpenGL3.RenderDrawData(_cachedDrawData);
    }

}