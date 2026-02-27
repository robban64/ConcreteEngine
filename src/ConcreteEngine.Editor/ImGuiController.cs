using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

internal sealed class ImGuiController(IWindow window, InputController input)
{
    public bool Initialized { get; private set; }

    private ImGuiContextPtr _imGuiContext;
    private ImGuiIOPtr _io;

    private ImDrawDataPtr _cachedDrawData;
    private bool _hasCachedDrawData;

    private float _scale;

    public unsafe void Setup(string fontFile, string iconFile, float scale)
    {
        if (Initialized) throw new InvalidOperationException("ImGuiRenderer already initialized");

        _scale = scale;

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        var io = _io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        //io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImGuiImplGLFW.SetCurrentContext(_imGuiContext);

        var windowPtr = (GLFWwindow*)window.Handle;
        ImGuiImplOpenGL3.SetCurrentContext(_imGuiContext);
        ImGuiImplOpenGL3.Init("#version 420");
        ImGuiImplGLFW.InitForOpenGL(windowPtr, false);

        ImGui.StyleColorsDark();

        io.DisplaySize = (Vector2)window.Size;
        io.DisplayFramebufferScale = Vector2.One;

        io.Fonts.Clear();
        GuiTheme.TextFont = io.Fonts.AddFontFromFileTTF(fontFile, GuiTheme.TextFontSize * _scale);
        GuiTheme.FontIconMedium = io.Fonts.AddFontFromFileTTF(iconFile, GuiTheme.IconMediumSize * _scale);
        GuiTheme.SetTheme(_scale);

        Initialized = true;
    }

    public void UpdateInputChar()
    {
        ref var io = ref _io;
        io.MousePos = input.Mouse.Position;
        io.MouseDown[0] = input.IsMouseDown(MouseButton.Left);
        io.MouseDown[1] = input.IsMouseDown(MouseButton.Right);
        io.MouseDown[2] = input.IsMouseDown(MouseButton.Middle);

        io.MouseWheel = input.Mouse.Scroll.Y;
        io.MouseWheelH = input.Mouse.Scroll.X;

        if (input is { HasEmptyKeyChars: true, HasEmptyKeyInput: true }) return;

        foreach (var key in input.GetActiveKeys())
            io.AddKeyEvent(ImGuiKeyMapper.AsImGuiKey(key), !input.IsKeyUp(key));

        foreach (var key in input.GetKeyChars())
            io.AddInputCharacter(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NewFrame(float deltaTime, Size2D windowSize)
    {
        _io = ImGui.GetIO();
        _io.DisplaySize = windowSize.ToVector2();
        _io.DisplayFramebufferScale = Vector2.One;
        _io.DeltaTime = deltaTime;

        ImGuiImplOpenGL3.NewFrame();
        ImGui.NewFrame();
        //ImGuizmo.BeginFrame();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndFrame()
    {
        ImGui.Render();
        _cachedDrawData = ImGui.GetDrawData();
        _hasCachedDrawData = true;

        var blockInput = _io.WantTextInput || ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused();
        var blockMouse = _io.WantCaptureMouse;

        input.ToggleBlockInput(blockInput || blockMouse);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderDrawData()
    {
        if (!_hasCachedDrawData || _cachedDrawData.DisplaySize is not { X: > 0, Y: > 0 }) return;
        ImGuiImplOpenGL3.RenderDrawData(_cachedDrawData);
    }
}