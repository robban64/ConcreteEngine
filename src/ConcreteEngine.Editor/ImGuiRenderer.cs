using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace ConcreteEngine.Editor;


internal sealed class ImGuiRenderer(IWindow window,EditorInputSource input)
{
    public bool Initialized { get; private set; }

    private ImGuiContextPtr _imGuiContext;

    private float _scale;
    
    public unsafe void Setup(string fontFile, float scale)
    {
        if (Initialized) throw new InvalidOperationException("ImGuiRenderer already initialized");

        _scale = scale;
        
        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImGuiImplGLFW.SetCurrentContext(_imGuiContext);


        var windowPtr = (GLFWwindow*)window.Handle;
        ImGuiImplOpenGL3.SetCurrentContext(_imGuiContext);
        ImGuiImplOpenGL3.Init("#version 330");
        ImGuiImplGLFW.InitForOpenGL(windowPtr, true);

        ImGui.StyleColorsDark();

        io.DisplaySize = (Vector2)window.Size;
        io.DisplayFramebufferScale = Vector2.One;

        io.Fonts.Clear();
        io.Fonts.AddFontFromFileTTF(fontFile, 14.0f * _scale);
        var style = ImGui.GetStyle();

        style.ScaleAllSizes(1);

        Initialized = true;
        
        //var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Roboto-Medium.ttf");
        //style.ScaleAllSizes(12);
        
        // ImGui.AddFontFromFileTTF(new )
        //ImGuiFontConfig fontConfDefault = new(fontPath, 14);

        
        /*
        _rateController = new RefreshRateController(_controller);
        args.InputCtx.Mice[0].Scroll += static (_, wheel) =>
        {
            EditorInput.OnMouseScroll(_, wheel);
            _rateController.WakeUp();
        };*/

    }

    public void BeginFrame(float deltaTime, Size2D windowSize)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = windowSize.ToVector2();
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = deltaTime;
        

        io.MousePos = input.Mouse.Position;
        io.MouseDown[0] = input.IsMouseDown(MouseButton.Left);
        io.MouseDown[1] = input.IsMouseDown(MouseButton.Right);
        io.MouseDown[2] = input.IsMouseDown(MouseButton.Middle);

        io.MouseWheel = input.Mouse.Scroll.Y;
        io.MouseWheelH = input.Mouse.Scroll.X;

        foreach (var key in input.KeysDown)
        {
            io.AddKeyEvent(ImGuiKeyMapper.ToImGuiKeySwitch(key), true);
        }
        ImGuiImplOpenGL3.NewFrame();
        ImGui.NewFrame();
    }

    public void EndFrame()
    {
        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
            window.MakeCurrent();
        }
        
        var drawData = ImGui.GetDrawData();
        
        if (drawData.DisplaySize is { X: > 0, Y: > 0 })
            ImGuiImplOpenGL3.RenderDrawData(drawData);
        

    }

   
}