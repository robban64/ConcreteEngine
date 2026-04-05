using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Configuration;
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

internal static unsafe class ImGuiSystem
{
    private const string FontFilename = "Roboto-Medium.ttf";
    private const string IconFilename = "lucide.ttf";
    public static bool Initialized { get; private set; }
    private static bool _hasCachedDrawData;
    public static Size2D OutputSize;

    public static ImGuiIOPtr Io;
    private static ImGuiContextPtr _imGuiContext;
    private static ImDrawDataPtr _cachedDrawData;

    public static ImGuiIO* IoPtr => Io.Handle;


    public static void Setup(IWindow window, float scale)
    {
        if (Initialized) throw new InvalidOperationException("ImGuiSystem already initialized");

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        Io = ImGui.GetIO();
        var io = IoPtr;
        io->ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        //io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImGuiImplGLFW.SetCurrentContext(_imGuiContext);

        var windowPtr = (GLFWwindow*)window.Handle;
        ImGuiImplOpenGL3.SetCurrentContext(_imGuiContext);
        ImGuiImplOpenGL3.Init("#version 420");
        ImGuiImplGLFW.InitForOpenGL(windowPtr, false);

        ImGuizmo.SetImGuiContext(_imGuiContext);

        ImGui.StyleColorsDark();

        io->DisplaySize = (Vector2)window.Size;
        io->DisplayFramebufferScale = Vector2.One;

        LoadFonts(scale);

        GuiTheme.SetTheme(scale);
        GuiTheme.SetImGuizmoTheme();

        ImGuizmo.Enable(true);

        Initialized = true;
    }


    public static void FillInput(InputController input)
    {
        var io = IoPtr;
        io->MousePos = input.Mouse.Position;
        io->MouseDown_0 = input.IsMouseDown(MouseButton.Left);
        io->MouseDown_1 = input.IsMouseDown(MouseButton.Right);
        io->MouseDown_2 = input.IsMouseDown(MouseButton.Middle);
        io->MouseWheel = input.Mouse.Scroll.Y;
        io->MouseWheelH = input.Mouse.Scroll.X;

        if (input is { HasEmptyKeyChars: true, HasEmptyKeyInput: true }) return;

        foreach (var key in input.GetActiveKeys())
            io->AddKeyEvent(ImGuiKeyMapper.AsImGuiKey(key), !input.IsKeyUp(key));

        foreach (var key in input.GetKeyChars())
            io->AddInputCharacter(key);
    }

    public static void NewFrame(float deltaTime, Size2D windowSize)
    {
        if (Io.IsNull) Io = ImGui.GetIO();
        var io = IoPtr;
        OutputSize = windowSize;
        io->DisplaySize = windowSize.ToVector2();
        io->DisplayFramebufferScale = Vector2.One;
        io->DeltaTime = deltaTime;

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


    private static void LoadFonts(float scale)
    {
        var buffer = stackalloc char[PathUtils.JoinPathLength];
        
        var fontSize = GuiTheme.FontSizeDefault * scale;
        var fonts = IoPtr->Fonts;
        fonts->Clear();
        
        var pathUtf8 = PathUtils.JoinPath(buffer, AppContext.BaseDirectory, EnginePath.ContentFolder, FontFilename);
        fonts->AddFontFromFileTTF(pathUtf8, fontSize);

        var glyphs = new Hexa.NET.ImGui.Utilities.GlyphRanges([0xe038, 0xf8ff, 0]);
        var config = ImGui.ImFontConfig().Handle;
        config->MergeMode = 1;
        config->PixelSnapH = 1;
        config->GlyphOffset.Y = 1f;
        
        pathUtf8 = PathUtils.JoinPath(buffer, AppContext.BaseDirectory, EnginePath.ContentFolder, IconFilename);
        GuiTheme.TextFont = fonts->AddFontFromFileTTF(pathUtf8, fontSize, config, glyphs.GetRanges());

        config->MergeMode = 0;
        config->GlyphOffset.Y = 0;
        config->GlyphMinAdvanceX = GuiTheme.IconSizeMedium * scale;
        GuiTheme.IconFont = fonts->AddFontFromFileTTF(pathUtf8, GuiTheme.IconSizeMedium * scale, config);

        fonts->CompactCache();
    }
}