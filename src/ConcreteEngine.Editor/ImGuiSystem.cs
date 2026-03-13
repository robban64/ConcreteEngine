using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;

internal static class ImGuiSystem
{
    public static bool Initialized { get; private set; }

    public static ImGuiIOPtr Io;

    private static ImGuiContextPtr _imGuiContext;
    private static ImDrawDataPtr _cachedDrawData;
    private static bool _hasCachedDrawData;

    private static float _scale;

    public static unsafe void Setup(IWindow window, float scale)
    {
        if (Initialized) throw new InvalidOperationException("ImGuiRenderer already initialized");
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Content", "lucide.ttf");

        _scale = scale;

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        var io = Io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        //io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImGuiImplGLFW.SetCurrentContext(_imGuiContext);

        var windowPtr = (GLFWwindow*)window.Handle;
        ImGuiImplOpenGL3.SetCurrentContext(_imGuiContext);
        ImGuiImplOpenGL3.Init("#version 420");
        ImGuiImplGLFW.InitForOpenGL(windowPtr, false);

        ImGuizmo.SetImGuiContext(_imGuiContext);

        ImGui.StyleColorsDark();

        io.DisplaySize = (Vector2)window.Size;
        io.DisplayFramebufferScale = Vector2.One;


        io.Fonts.Clear();

        float fontSize = GuiTheme.FontSizeDefault * _scale;
        io.Fonts.AddFontFromFileTTF(fontPath, fontSize);

        var glyphs = new Hexa.NET.ImGui.Utilities.GlyphRanges([0xe038, 0xf8ff, 0]);
        var config = ImGui.ImFontConfig();
        config.MergeMode = true;
        config.PixelSnapH = true;
        config.GlyphOffset.Y = 1f;
        GuiTheme.TextFont = io.Fonts.AddFontFromFileTTF(iconPath, fontSize, config.Handle, glyphs.GetRanges());


        config.MergeMode = false;
        config.GlyphOffset.Y = 0;
        config.GlyphMinAdvanceX = GuiTheme.IconSizeMedium * _scale;
        GuiTheme.IconFont = io.Fonts.AddFontFromFileTTF(iconPath, GuiTheme.IconSizeMedium * _scale, config);

        io.Fonts.CompactCache();

        GuiTheme.SetTheme(_scale);
        GuiTheme.SetImGuizmoTheme();

        ImGuizmo.Enable(true);

        Initialized = true;
    }

    public static void FillInput(InputController input)
    {
        ref var io = ref Io;
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
    public static void NewFrame(float deltaTime, Size2D windowSize)
    {
        if(Io.IsNull) Io = ImGui.GetIO();
        Io.DisplaySize = windowSize.ToVector2();
        Io.DisplayFramebufferScale = Vector2.One;
        Io.DeltaTime = deltaTime;

        ImGuiImplOpenGL3.NewFrame();
        ImGui.NewFrame();
        ImGuizmo.BeginFrame();
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.SetRect(0, 0, windowSize.Width, windowSize.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EndFrame()
    {
        ImGui.Render();
        _cachedDrawData = ImGui.GetDrawData();
        _hasCachedDrawData = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RenderDrawData()
    {
        if (!_hasCachedDrawData || _cachedDrawData.DisplaySize is not { X: > 0, Y: > 0 }) return;
        ImGuiImplOpenGL3.RenderDrawData(_cachedDrawData);
    }
}