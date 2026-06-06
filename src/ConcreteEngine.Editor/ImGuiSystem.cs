using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Visuals;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;

namespace ConcreteEngine.Editor;

internal static unsafe class ImGuiSystem
{
    private const string FontFilename = "Roboto-Medium.ttf";
    private const string IconFilename = "lucide.ttf";
    public static bool Initialized { get; private set; }
    private static bool _hasCachedDrawData;

    public static TextureId OutputTexture;
    
    public static ImGuiViewportPtr MainViewportPtr;

    public static ImGuiIOPtr Io;
    private static ImDrawDataPtr _cachedDrawData;

    private static ImGuiIO* IoPtr => Io.Handle;

    public static void Setup( float scale)
    {
        if (Initialized) throw new InvalidOperationException("ImGuiSystem already initialized");

        var imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiContext);

        Io = ImGui.GetIO();
        var io = IoPtr;
        io->IniFilename = null;
        io->DisplaySize.X = EngineWindow.WindowSize.Width;
        io->DisplaySize.Y = EngineWindow.WindowSize.Height;
        io->ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.IsSrgb |
                           ImGuiConfigFlags.DockingEnable;


        ImGuiImplGLFW.SetCurrentContext(imGuiContext);

        var windowPtr = (GLFWwindow*)EngineWindow.PlatformWindowPtr;
        ImGuiImplOpenGL3.SetCurrentContext(imGuiContext);
        ImGuiImplOpenGL3.Init("#version 420"u8);
        ImGuiImplGLFW.InitForOpenGL(windowPtr, false);

        ImGuizmo.SetImGuiContext(imGuiContext);

        ImGui.StyleColorsDark();

        LoadFonts(scale);

        GuiTheme.SetTheme(scale);
        GuiTheme.SetImGuizmoTheme();

        ImGuizmo.Enable(true);

        Initialized = true;
    }


    public static void FillInput()
    {
        var io = IoPtr;
        io->MousePos = EngineInput.Mouse.ScreenPos;
        io->MouseWheel = EngineInput.Mouse.Scroll.Y;
        io->MouseWheelH = EngineInput.Mouse.Scroll.X;
        io->MouseDown_0 = EditorInput.Layer.IsMouseDown(MouseButton.Left);
        io->MouseDown_1 = EditorInput.Layer.IsMouseDown(MouseButton.Right);
        io->MouseDown_2 = EditorInput.Layer.IsMouseDown(MouseButton.Middle);

        if (EngineInput.Keyboard.HasEmptyKeyInput && EngineInput.Keyboard.HasEmptyKeyChars) return;

        foreach (var key in EngineInput.Keyboard.GetActiveKeys())
            io->AddKeyEvent(ImGuiKeyMapper.AsImGuiKey(key), !EditorInput.Layer.IsKeyUp(key));

        foreach (var key in EngineInput.Keyboard.GetKeyChars())
            io->AddInputCharacter(key);
    }

    public static void NewFrame(float deltaTime, TextureId outputTexture)
    {
        OutputTexture = outputTexture;

        if (Io.IsNull) Io = ImGui.GetIO();
        var io = IoPtr;
        io->DisplaySize = EngineWindow.OutputSize.ToVector2();
        io->DisplayFramebufferScale = Vector2.One;
        io->DeltaTime = deltaTime;

        ImGuiImplOpenGL3.NewFrame();
        ImGui.NewFrame();

        MainViewportPtr = ImGui.GetMainViewport();
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

        var pathUtf8 = PathUtils.JoinPath(buffer, "./", EnginePath.EditorContentPath, FontFilename);
        fonts->AddFontFromFileTTF(pathUtf8, fontSize);

        var glyphs = new Hexa.NET.ImGui.Utilities.GlyphRanges([0xe038, 0xf8ff, 0]);
        var config = ImGui.ImFontConfig().Handle;
        config->MergeMode = 1;
        config->PixelSnapH = 1;
        config->GlyphOffset.Y = 1f;

        pathUtf8 = PathUtils.JoinPath(buffer, "./", EnginePath.EditorContentPath, IconFilename);
        GuiTheme.TextFont = fonts->AddFontFromFileTTF(pathUtf8, fontSize, config, glyphs.GetRanges());

        config->MergeMode = 0;
        config->GlyphOffset.Y = 0;
        config->GlyphMinAdvanceX = GuiTheme.IconSizeMedium * scale;
        GuiTheme.IconFont = fonts->AddFontFromFileTTF(pathUtf8, GuiTheme.IconSizeMedium * scale, config);

        fonts->CompactCache();
    }
}