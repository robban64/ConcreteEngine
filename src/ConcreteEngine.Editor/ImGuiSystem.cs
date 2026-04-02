using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Input;
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
    private static bool _hasCachedDrawData;
    public static Size2D OutputSize;

    public static ImGuiIOPtr Io;
    private static ImGuiContextPtr _imGuiContext;
    private static ImDrawDataPtr _cachedDrawData;


    public static unsafe void Setup(IWindow window, float scale)
    {
        if (Initialized) throw new InvalidOperationException("ImGuiSystem already initialized");

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

        LoadFonts(scale);

        GuiTheme.SetTheme(scale);
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

    public static void NewFrame(float deltaTime, Size2D windowSize)
    {
        if (Io.IsNull) Io = ImGui.GetIO();
        OutputSize = windowSize;
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


    private static unsafe void LoadFonts(float scale)
    {
        Span<char> charBuffer = stackalloc char[512];
        var byteBuffer = stackalloc byte[512];
        var pathPtr = new NativeViewPtr<byte>(byteBuffer, 0, 512);

        float fontSize = GuiTheme.FontSizeDefault * scale;

        Path.TryJoin(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf", charBuffer, out var written);
        var path = charBuffer.Slice(0, written);

        Io.Fonts.Clear();
        Io.Fonts.AddFontFromFileTTF(pathPtr.Writer().Write(path), fontSize);

        Path.TryJoin(AppContext.BaseDirectory, "Content", "lucide.ttf", charBuffer, out written);
        path = charBuffer.Slice(0, written);
        pathPtr.Writer().Write(path);


        var glyphs = new Hexa.NET.ImGui.Utilities.GlyphRanges([0xe038, 0xf8ff, 0]);
        var config = ImGui.ImFontConfig();
        config.MergeMode = true;
        config.PixelSnapH = true;
        config.GlyphOffset.Y = 1f;
        GuiTheme.TextFont = Io.Fonts.AddFontFromFileTTF(pathPtr, fontSize, config.Handle, glyphs.GetRanges());

        config.MergeMode = false;
        config.GlyphOffset.Y = 0;
        config.GlyphMinAdvanceX = GuiTheme.IconSizeMedium * scale;
        GuiTheme.IconFont = Io.Fonts.AddFontFromFileTTF(pathPtr, GuiTheme.IconSizeMedium * scale, config);

        Io.Fonts.CompactCache();
    }
}